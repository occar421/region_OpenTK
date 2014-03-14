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
			GraphicsMode mode = new GraphicsMode(

				//ColorFormat構造体を用いて、各色のピクセル当たりのビット数(カラーバッファのサイズ)
				GraphicsMode.Default.ColorFormat,

				//デプスバッファのサイズ
				GraphicsMode.Default.Depth,

				//ステンシルバッファのサイズ
				GraphicsMode.Default.Stencil,

				//AA(AntiAliasing)のサイズ x4 x8などの数字
				GraphicsMode.Default.Samples,

				//ColorFormat構造体を用いて、アキュムレーションバッファのサイズ
				GraphicsMode.Default.AccumulatorFormat,

				//バッファリングに使うフレームバッファの数 1(シングルバッファリング),2(ダブル-),3(トリプル-)
				GraphicsMode.Default.Buffers,

				//ステレオ投影をするかどうか
				GraphicsMode.Default.Stereo);

			using (Game window = new Game(mode))
			{
				window.Run(30.0);
			}
			return 0;
		}
	}

	class Game : GameWindow
	{
		//800x600のウィンドウを作る。タイトルは「0-7:GraphicsMode」
		public Game(GraphicsMode mode)
			: base(800, 600, mode, "0-7:GraphicsMode")
		{
			VSync = VSyncMode.On;
		}

		//ウィンドウの起動時に実行される。
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			GL.ClearColor(Color4.Black);
			GL.Enable(EnableCap.DepthTest);
		}

		//ウィンドウのサイズが変更された場合に実行される。
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
			GL.MatrixMode(MatrixMode.Projection);
			Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, (float)Width / (float)Height, 1.0f, 64.0f);
			GL.LoadMatrix(ref projection);
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
		}

		//画面描画で実行される。
		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			GL.MatrixMode(MatrixMode.Modelview);
			Matrix4 modelview = Matrix4.LookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
			GL.LoadMatrix(ref modelview);

			GL.Begin(BeginMode.Quads);

			GL.Color4(Color4.White);							//色名で指定
			GL.Vertex3(-1f, 1.0f, 4.0f);
			GL.Color4(new float[] { 1.0f, 0.0f, 0.0f, 1.0f });	//配列で指定
			GL.Vertex3(-1.0f, -1.0f, 4.0f);
			GL.Color4(0.0f, 1.0f, 0.0f, 1.0f);					//4つの引数にfloat型で指定
			GL.Vertex3(1.0f, -1.0f, 4.0f);
			GL.Color4((byte)0, (byte)0, (byte)255, (byte)255);	//byte型で指定
			GL.Vertex3(1.0f, 1.0f, 4.0f);

			GL.End();

			SwapBuffers();
		}
	}
}