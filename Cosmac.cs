using Raylib_cs;

namespace Chip8;

public class Cosmac : IDisposable
{
	readonly static bool debug = false;
	public const int GFX_Width = 64;
	public const int GFX_Height = 32;
	public const int GFX_Size = GFX_Width * GFX_Height;
	public const int RAMSize = 4096;

	private static readonly byte[] Fontset = {
		0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
		0x20, 0x60, 0x20, 0x20, 0x70, // 1
		0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
		0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
		0x90, 0x90, 0xF0, 0x10, 0x10, // 4
		0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
		0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
		0xF0, 0x10, 0x20, 0x40, 0x40, // 7
		0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
		0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
		0xF0, 0x90, 0xF0, 0x90, 0x90, // A
		0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
		0xF0, 0x80, 0x80, 0x80, 0xF0, // C
		0xE0, 0x90, 0x90, 0x90, 0xE0, // D
		0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
		0xF0, 0x80, 0xF0, 0x80, 0x80  // F
	};

	private static readonly KeyboardKey[] KeyMap = {
		KeyboardKey.X,
		KeyboardKey.One, KeyboardKey.Two, KeyboardKey.Three,
		KeyboardKey.Q, KeyboardKey.W, KeyboardKey.E,
		KeyboardKey.A, KeyboardKey.S, KeyboardKey.D,
		KeyboardKey.Z, KeyboardKey.C, KeyboardKey.Four,
		KeyboardKey.R, KeyboardKey.F, KeyboardKey.V
	};

	private class Hardware
	{
		public byte[] Memory = new byte[RAMSize];
		public byte[] GFX = new byte[GFX_Size];
		public byte[] V = new byte[16];
		public byte[] Key = new byte[16];
		public ushort[] Stack = new ushort[16];
		public ushort SP = 0;

		public ushort PC = 0x200;
		public ushort IDX = 0;
		public ushort Opcode = 0;

		public byte Delay = 0;
		public byte Sound = 0;

		public bool Draw = false;
		public bool Running = true;

		public Random RNG = new();

		public void ClearDisplay()
		{
			Array.Clear(GFX, 0, GFX.Length);
			Draw = true;
		}

		public void FetchOpcode()
		{
			Opcode = (ushort)(Memory[PC] << 8);
			Opcode |= Memory[PC + 1];
		}

		public ushort GetAddress()
		{
			return (ushort)(Opcode & 0x0FFF);
		}

		public byte GetByte()
		{
			return (byte)(Opcode & 0x00FF);
		}

		public byte GetN()
		{
			return (byte)(Opcode & 0x000F);
		}

		public byte GetX()
		{
			return (byte)((Opcode & 0x0F00) >> 8);
		}

		public byte GetY()
		{
			return (byte)((Opcode & 0x00F0) >> 4);
		}

		public byte GetKey()
		{
			return Key[GetVX() & 0xF];
		}

		public byte GetVX()
		{
			return V[GetX()];
		}

		public byte GetVY()
		{
			return V[GetY()];
		}

		public void SetVX(byte num)
		{
			V[GetX()] = num;
		}

		public void SetVY(byte num)
		{
			V[GetY()] = num;
		}

		public void IncreasePC()
		{
			PC += 2;
		}

		public void DecreasePC()
		{
			PC -= 2;
		}

		public void SubroutineCall()
		{
			Stack[SP++] = PC;
			PC = GetAddress();
		}

		public void SubroutineReturn()
		{
			PC = Stack[--SP];
		}

		public void JumpAddress()
		{
			PC = GetAddress();
		}

		public void JumpAddressPlus()
		{
			PC = (ushort)(GetAddress() + V[0]);
		}

		public void SkipInstruction(bool condition)
		{
			if (condition)
				IncreasePC();
		}

		public void Rand()
		{
			V[GetX()] = (byte)(RNG.Next(0xFF) & GetByte());
		}

		public void DrawSprite()
		{
			byte VX = GetVX();
			byte VY = GetVY();
			byte N = GetN();
			byte pixel;

			V[0xF] = 0;

			int x = VX & 0x3F;
			int y = VY & 0x1F;

			for (int yLine = 0; yLine < N; yLine++)
			{
				pixel = Memory[IDX + yLine];
				for (int xLine = 0; xLine < 8; xLine++)
				{
					if ((pixel & (0x80 >> xLine)) == 0)
						continue;

					int pixelX = x + xLine;
					int pixelY = y + yLine;
					int index = pixelY * GFX_Width + pixelX;

					if (pixelX >= 64 || pixelY >= 32)
						continue;

					if (GFX[index] == 1)
						V[0xF] = 1;

					GFX[index] ^= 1;
				}
			}

			Draw = true;
		}

		public void KeyBlock()
		{
			for (byte i = 0; i < 0xF; i++)
			{
				if (Key[i] == 1)
				{
					V[GetX()] = i;
					Key[i] = 0;
					return;
				}
			}

			DecreasePC();
		}

		public void StoreV()
		{
			for (int i = 0; i <= GetX(); i++)
			{
				Memory[IDX++] = V[i];
			}
		}

		public void LoadV()
		{
			for (int i = 0; i <= GetX(); i++)
			{
				V[i] = Memory[IDX++];
			}
		}

		public void BCD()
		{
			byte VX = GetVX();
			Memory[IDX] = (byte)(VX / 100);
			Memory[IDX + 1] = (byte)(VX / 10 % 10);
			Memory[IDX + 2] = (byte)(VX % 100 % 10);
		}

		public void ASSIGN()
		{
			V[GetX()] = V[GetY()];
		}

		public void ADD()
		{
			ushort VY = GetVY();
			ushort VX = GetVX();

			V[GetX()] += (byte)VY;
			V[0xF] = (byte)((VX + VY > 255) ? 1 : 0);
		}

		public void SUB()
		{
			byte VY = GetVY();
			byte VX = GetVX();

			V[GetX()] -= VY;
			V[0xF] = (byte)((VX >= VY) ? 1 : 0);
		}

		public void SUBV()
		{
			byte VY = GetVY();
			byte VX = GetVX();

			V[GetX()] = (byte)(VY - VX);
			V[0xF] = (byte)((VY >= VX) ? 1 : 0);
		}

		public void OR()
		{
			V[GetX()] |= V[GetY()];
			V[0xF] = 0;
		}

		public void AND()
		{
			V[GetX()] &= V[GetY()];
			V[0xF] = 0;
		}

		public void XOR()
		{
			V[GetX()] ^= V[GetY()];
			V[0xF] = 0;
		}

		public void RSHIFT()
		{
			byte VY = GetVY();
			V[GetX()] = (byte)(VY >> 1);
			V[0xF] = (byte)(VY & 1);
		}

		public void LSHIFT()
		{
			byte VY = GetVY();
			V[GetX()] = (byte)(VY << 1);
			V[0xF] = (byte)((VY & 0x80) != 0 ? 1 : 0);
		}

		public void DebugPrint()
		{
			if (!Cosmac.debug)
				return;

			Console.Write($"PC: 0x{PC:X4} - ");
			Console.Write($"Opcode: 0x{Opcode:X4} - ");
			Console.Write($"V{GetX():X1}: 0x{GetVX():X4} - ");
			Console.Write($"V{GetY():X1}: 0x{GetVY():X4} - ");
			Console.Write($"Address: 0x{GetAddress():X3} - ");
			Console.Write($"Byte: 0x{GetByte():X2} - ");
			Console.Write($"N: 0x{GetN():X1} - ");
			Console.Write($"SP: {SP} - ");
			Console.Write($"IDX: {IDX}\n");

			Console.ReadKey();
		}
	}

	private Hardware HW { get; set; } = new Hardware();
	public Sound Beep { get; set; }

	public Cosmac()
	{
		for (int i = 0; i < 80; i++)
		{
			HW.Memory[i] = Fontset[i];
		}
	}

	public void Dispose()
	{
		Raylib.UnloadSound(Beep);
		GC.SuppressFinalize(this);
	}

	private void PlayBeep()
	{
		Raylib.PlaySound(Beep);
	}

	public bool LoadGame(string rom)
	{
		try
		{
			using FileStream fs = File.OpenRead(rom);
			int bytesRead = fs.Read(HW.Memory, 0x200, HW.Memory.Length - 0x200);
			if (bytesRead == 0)
			{
				Console.WriteLine("Could not read ROM");
				return false;
			}
		}
		catch (Exception)
		{
			Console.WriteLine("Could not load ROM");
			return false;
		}

		Console.WriteLine("Rom loaded!");
		return true;
	}

	public void LoadSound()
	{
		Beep = Raylib.LoadSound("res/beep.wav");
	}


	public void Cycle()
	{
		HW.FetchOpcode();
		HW.IncreasePC();
		HW.DebugPrint();

		switch (HW.Opcode & 0xF000)
		{
			case 0x0000:
				switch (HW.Opcode & 0x00FF)
				{
					case 0x00E0:
						HW.ClearDisplay();
						break;
					case 0x00EE:
						HW.SubroutineReturn();
						break;
					default:
						LogUnkownOpcode();
						break;
				}
				break;
			case 0x1000:
				HW.JumpAddress();
				break;
			case 0x2000:
				HW.SubroutineCall();
				break;
			case 0x3000:
				HW.SkipInstruction(HW.GetVX() == HW.GetByte());
				break;
			case 0x4000:
				HW.SkipInstruction(HW.GetVX() != HW.GetByte());
				break;
			case 0x5000:
				HW.SkipInstruction(HW.GetVX() == HW.GetVY());
				break;
			case 0x6000:
				HW.SetVX(HW.GetByte());
				break;
			case 0x7000:
				HW.SetVX((byte)(HW.GetByte() + HW.GetVX()));
				break;
			case 0x8000:
				switch (HW.Opcode & 0x000F)
				{
					case 0x0:
						HW.ASSIGN();
						break;
					case 0x1:
						HW.OR();
						break;
					case 0x2:
						HW.AND();
						break;
					case 0x3:
						HW.XOR();
						break;
					case 0x4:
						HW.ADD();
						break;
					case 0x5:
						HW.SUB();
						break;
					case 0x6:
						HW.RSHIFT();
						break;
					case 0x7:
						HW.SUBV();
						break;
					case 0xE:
						HW.LSHIFT();
						break;
					default:
						LogUnkownOpcode();
						break;
				}
				break;
			case 0x9000:
				HW.SkipInstruction(HW.GetVX() != HW.GetVY());
				break;
			case 0xA000:
				HW.IDX = HW.GetAddress();
				break;
			case 0xB000:
				HW.JumpAddressPlus();
				break;
			case 0xC000:
				HW.Rand();
				break;
			case 0xD000:
				HW.DrawSprite();
				break;
			case 0xE000:
				switch (HW.GetByte())
				{
					case 0x9E:
						HW.SkipInstruction(HW.GetKey() == 1);
						break;
					case 0xA1:
						HW.SkipInstruction(HW.GetKey() == 0);
						break;
					default:
						LogUnkownOpcode();
						break;
				}
				break;
			case 0xF000:
				switch (HW.Opcode & 0x00FF)
				{
					case 0x0007:
						HW.SetVX(HW.Delay);
						break;
					case 0x000A:
						HW.KeyBlock();
						break;
					case 0x0015:
						HW.Delay = HW.GetVX();
						break;
					case 0x0018:
						HW.Sound = HW.GetVX();
						break;
					case 0x001E:
						HW.IDX += HW.GetVX();
						break;
					case 0x0029:
						HW.IDX = (ushort)((HW.GetVX() & 0x0F) * 5);
						break;
					case 0x0033:
						HW.BCD();
						break;
					case 0x0055:
						HW.StoreV();
						break;
					case 0x0065:
						HW.LoadV();
						break;
					default:
						LogUnkownOpcode();
						break;
				}
				break;
			default:
				LogUnkownOpcode();
				break;
		}

		if (HW.Delay > 0)
			HW.Delay--;

		if (HW.Sound == 1)
			PlayBeep();

		if (HW.Sound > 0)
			HW.Sound--;
	}

	public void SetKeys()
	{
		if (Raylib.IsKeyPressed(KeyboardKey.Escape))
		{
			HW.Running = false;
			return;
		}

		for (int i = 0; i <= 0xF; i++)
		{
			if (Raylib.IsKeyDown(KeyMap[i]))
				HW.Key[i] = 1;
			else if (Raylib.IsKeyUp(KeyMap[i]))
				HW.Key[i] = 0;
		}
	}

	public bool ShouldClose()
	{
		return !HW.Running;
	}

	public bool ShouldDraw()
	{
		return HW.Draw;
	}

	public void ResetDraw()
	{
		HW.Draw = false;
	}

	public byte[] GetGFX()
	{
		return HW.GFX;
	}

	private void LogUnkownOpcode()
	{
		Console.Write($"Unkown Opcode 0x{HW.Opcode:X4} ");
		Console.WriteLine($"At PC: 0x{HW.PC:4}");
	}
}
