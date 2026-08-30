using System.Numerics;
using Raylib_cs;

namespace Chip8;

public class Emulator : IDisposable
{
	public Cosmac Chip8;

	public int Width;
	public int Height;
	public const int Framerate = 60;
	public int Cycles;

	private Rectangle source;
	private Rectangle dest;
	private Vector2 vec;

	public byte[] Pixels = new byte[Cosmac.GFX_Size];
	public Image Img;
	public Texture2D Texture;

	public Emulator(string rom, int cycles, int resolution)
	{
		Cycles = cycles;

		Width = Cosmac.GFX_Width * resolution;
		Height = Cosmac.GFX_Height * resolution;

		Chip8 = new();
		if (Chip8.LoadGame(rom) == false)
			throw new FileLoadException("ROM not valid");

		Raylib.InitWindow(Width, Height, "Chip8 C#");
		Raylib.InitAudioDevice();
		Raylib.SetTargetFPS(Framerate);

		Chip8.LoadSound();

		unsafe
		{
			fixed (byte* pixels = Pixels)
			{
				Img.Data = pixels;
				Img.Width = Cosmac.GFX_Width;
				Img.Height = Cosmac.GFX_Height;
				Img.Format = PixelFormat.UncompressedGrayscale;
				Img.Mipmaps = 1;

				Texture = Raylib.LoadTextureFromImage(Img);
			}
		}

		source = new(0, 0, Cosmac.GFX_Width, Cosmac.GFX_Height);
		dest = new(0, 0, Width, Height);
		vec = new(0, 0);
	}

	public void Dispose()
	{
		Chip8.Dispose();
		Raylib.CloseWindow();
		Raylib.CloseAudioDevice();

		GC.SuppressFinalize(this);
	}

	public void DrawFrame()
	{
		if (Chip8.ShouldDraw())
		{
			byte[] gfx = Chip8.GetGFX();

			for (int i = 0; i < Cosmac.GFX_Size; i++)
				Pixels[i] = (byte)(gfx[i] == 1 ? 255 : 0);

			Raylib.UpdateTexture(Texture, Pixels);
		}

		Raylib.BeginDrawing();

		Raylib.ClearBackground(Color.Black);
		Raylib.DrawTexturePro(Texture, source, dest, vec, 0, Color.White);

		Raylib.EndDrawing();

		Chip8.ResetDraw();
	}

	public void Loop()
	{
		while (!Raylib.WindowShouldClose())
		{
			Chip8.SetKeys();
			if (Chip8.ShouldClose())
				break;

			if (Raylib.IsKeyPressed(KeyboardKey.Up) && Cycles < 100)
			{
				Cycles++;
				Console.WriteLine($"Increased Cycles to: {Cycles}");
			}
			else if (Raylib.IsKeyPressed(KeyboardKey.Down) && Cycles > 1)
			{
				Cycles--;
				Console.WriteLine($"Decreased Cycles to {Cycles}");
			}

			for (int i = 0; i < Cycles; i++)
				Chip8.Cycle();

			DrawFrame();
		}
	}
}

class Program
{
	static int Main()
	{
		try
		{
			Console.Write("Please type the path to your ROM: ");
			string? rom = Console.ReadLine();
			if (rom == null || rom.Length == 0)
			{
				Console.WriteLine("Invalid rom");
				return 1;
			}

			Console.Write("How many cycles? (1 to 100): ");
			int cycles = Convert.ToUInt16(Console.ReadLine());
			if (cycles <= 0)
				cycles = 1;
			else if (cycles >= 100)
				cycles = 100;

			Console.Write("Screen resolution? (1x: 64x32) (1 to 20): ");
			int resolution = Convert.ToInt16(Console.ReadLine());
			if (resolution <= 0)
				resolution = 1;
			else if (resolution >= 20)
				resolution = 20;

			Emulator e = new(rom, cycles, resolution);

			e.Loop();
			e.Dispose();
		}
		catch (Exception)
		{
			Console.WriteLine("Evil");
			return 2;
		}

		Console.WriteLine("Bye!");
		return 0;
	}
}
