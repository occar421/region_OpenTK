/*
 * 参照設定 : OpenTK System.Drawing
 */

using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace OpenTK_Sample
{
	class OpenTK_Sample
	{
		[STAThread]
		static int Main()
		{
			using (Game window = new Game())
			{
				window.Run(30.0);
			}
			return 0;
		}
	}

	class Game : GameWindow
	{
		Vector2[] Position;		//点の位置
		Color4[] Color;			//点の色
		const int N = 130;		//点の数

		//800x600のウィンドウを作る。タイトルは「1-1:Points and Lines」
		public Game()
			: base(800, 600, GraphicsMode.Default, "1-1:Points and Lines")
		{
			VSync = VSyncMode.On;
			Position = new Vector2[N];
			Color = new Color4[N];
		}

		//ウィンドウの起動時に実行される。
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			GL.ClearColor(Color4.Black);
			GL.Enable(EnableCap.DepthTest);

			GL.PointSize(3.0f);		//点の大きさを変更

			GL.LineWidth(1.5f);		//線の太さを変更

			Random();
		}

		//ウィンドウのサイズが変更された場合に実行される。
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			GL.Viewport(ClientRectangle);
		}

		//画面更新で実行される。
		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);

			//Escapeキーで終了
			if (Keyboard[Key.Escape])
			{
				this.Exit();
			}

			//Enterキーで点をランダムにする
			if (Keyboard[Key.Enter])
			{
				Random();
			}
		}

		//画面描画で実行される。
		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			//点をすべて描画
			GL.Begin(BeginMode.Points);
			for (int i = 0; i < N; i++)
			{
				GL.Color4(Color[i]);
				GL.Vertex2(Position[i]);
			}
			GL.End();

			//線をすべて描画
			GL.Begin(BeginMode.Lines);
			for (int i = 0; i < N; i++)
			{
				GL.Color4(Color[i]);
				GL.Vertex2(Position[i]);
			}
			GL.End();

			SwapBuffers();
		}

		//ランダムに値を代入
		void Random()
		{
			Random r = new Random();
			for (int i = 0; i < N; i++)
			{
				Position[i].X = (float)r.NextDouble() * 2.0f - 1.0f;
				Position[i].Y = (float)r.NextDouble() * 2.0f - 1.0f;
				Color[i].R = (float)r.NextDouble();
				Color[i].G = (float)r.NextDouble();
				Color[i].B = (float)r.NextDouble();
			}
		}
	}
}