/**
 * 参照設定 : OpenTK OpenTK.GLControl System System.Drawing System.Windows.Forms
 */

using System;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace OpenTK_Sample
{
	public partial class WinForm : Form
	{
		//回転角を保持する変数
		float deg1 = 0.0f;
		float deg2 = 0.0f;
		public WinForm()
		{
			//(デザイナで指定)800x600のウィンドウを作る。タイトルは「0-4:WindowsForm」
			InitializeComponent();
			//プログラムが暇なときに動作する
			Application.Idle += (sender, e) =>
			{
				//2つのコントロールがアイドル状態なら両方描画
				while (glControl1.IsIdle && glControl2.IsIdle)
				{
					Render1();

					deg1 += (float)numericUpDown1.Value;
					if (deg1 > 360.0f)
						deg1 -= 360.0f;
					if (deg1 < 0.0f)
						deg1 += 360.0f;

					Render2();

					deg2 += (float)numericUpDown2.Value;
					if (deg2 > 360.0f)
						deg2 -= 360.0f;
					if (deg2 < 0.0f)
						deg2 += 360.0f;
				}
			};
		}

		//glControl1の起動時に実行される。
		private void glControl1_Load(object sender, EventArgs e)
		{
			glControl1.MakeCurrent();	//OpenTKの処理先をglControl1に変更

			GL.ClearColor(Color4.Black);
			GL.Enable(EnableCap.DepthTest);
		}

		//glControl1のサイズ変更時に実行される。
		private void glControl1_Resize(object sender, EventArgs e)
		{
			glControl1.MakeCurrent();	//OpenTKの処理先をglControl1に変更

			GL.Viewport(0, 0, glControl1.Size.Width, glControl1.Size.Height);
			GL.MatrixMode(MatrixMode.Projection);
			Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, (float)glControl1.Size.Width / (float)glControl1.Size.Height, 1.0f, 64.0f);
			GL.LoadMatrix(ref projection);
		}

		//glControl1の描画関数
		private void Render1()
		{
			glControl1.MakeCurrent();	//OpenTKの処理先をglControl1に変更

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			GL.MatrixMode(MatrixMode.Modelview);
			Matrix4 modelview = Matrix4.LookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
			GL.LoadMatrix(ref modelview);

			GL.Rotate(deg1, Vector3.UnitZ);

			GL.Begin(BeginMode.Quads);

			GL.Color4(Color4.White);
			GL.Vertex3(-1.0f, 1.0f, 4.0f);
			GL.Color4(Color4.Red);
			GL.Vertex3(-1.0f, -1.0f, 4.0f);
			GL.Color4(Color4.Lime);
			GL.Vertex3(1.0f, -1.0f, 4.0f);
			GL.Color4(Color4.Blue);
			GL.Vertex3(1.0f, 1.0f, 4.0f);

			GL.End();
			glControl1.SwapBuffers();
		}

		//glControl2の起動時に実行される
		private void glControl2_Load(object sender, EventArgs e)
		{
			glControl2.MakeCurrent();	//OpenTKの処理先をglControl2に変更

			GL.ClearColor(Color4.Black);
			GL.Enable(EnableCap.DepthTest);
		}

		//glControl2のサイズ変更時に実行される
		private void glControl2_Resize(object sender, EventArgs e)
		{
			glControl2.MakeCurrent();	//OpenTKの処理先をglControl2に変更

			GL.Viewport(0, 0, glControl2.Size.Width, glControl2.Size.Height);
			GL.MatrixMode(MatrixMode.Projection);
			Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, (float)glControl2.Size.Width / (float)glControl2.Size.Height, 1.0f, 64.0f);
			GL.LoadMatrix(ref projection);

		}

		//glControl2の描画関数
		private void Render2()
		{
			glControl2.MakeCurrent();	//OpenTKの処理先をglControl2に変更

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			GL.MatrixMode(MatrixMode.Modelview);
			Matrix4 modelview = Matrix4.LookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
			GL.LoadMatrix(ref modelview);

			GL.Rotate(deg2, Vector3.UnitZ);

			GL.Begin(BeginMode.Quads);

			GL.Color4(Color4.White);
			GL.Vertex3(-1.0f, 1.0f, 4.0f);
			GL.Color4(Color4.Red);
			GL.Vertex3(-1.0f, -1.0f, 4.0f);
			GL.Color4(Color4.Lime);
			GL.Vertex3(1.0f, -1.0f, 4.0f);
			GL.Color4(Color4.Blue);
			GL.Vertex3(1.0f, 1.0f, 4.0f);

			GL.End();
			glControl2.SwapBuffers();
		}
	}

	class OpenTK_Sample
	{
		[STAThread]
		static int Main()
		{
			using (WinForm window = new WinForm())
			{
				window.ShowDialog();
			}
			return 0;
		}
	}
}
