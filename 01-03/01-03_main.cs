/*
 * 参照設定 : OpenTK System System.Drawing
 */

using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace OpenTK_Samples
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
		#region Camera__Field

		bool isCameraRotating;		//カメラが回転状態かどうか
		Vector2 current, previous;	//現在の点、前の点
		Matrix4 rotate;				//回転行列
		float zoom;					//拡大度
		float wheelPrevious;		//マウスホイールの前の状態

		#endregion

		Vector3[] position;	//頂点の位置
		const int N = 200;	//頂点の数

		uint vbo;			//VBOのバッファの識別番号を保持

		//800x600のウィンドウを作る。タイトルは「1-3:VertexBufferObject(1)」
		public Game()
			: base(800, 600, GraphicsMode.Default, "1-3:VertexBufferObject(1)")
		{
			position = new Vector3[N];
			vbo = 0;

			#region Camera__Initialize

			isCameraRotating = false;
			current = Vector2.Zero;
			previous = Vector2.Zero;
			rotate = Matrix4.Identity;
			zoom = 1.0f;
			wheelPrevious = 0.0f;

			#endregion

			#region Camera__Event

			//マウスボタンが押されると発生するイベント
			this.Mouse.ButtonDown += (sender, e) =>
			{
				//右ボタンが押された場合
				if (e.Button == MouseButton.Right)
				{
					isCameraRotating = true;
					current = new Vector2(this.Mouse.X, this.Mouse.Y);
				}
			};

			//マウスボタンが離されると発生するイベント
			this.Mouse.ButtonUp += (sender, e) =>
			{
				//右ボタンが押された場合
				if (e.Button == MouseButton.Right)
				{
					isCameraRotating = false;
					previous = Vector2.Zero;
				}
			};

			//マウスが動くと発生するイベント
			this.Mouse.Move += (sender, e) =>
			{
				////カメラが回転状態の場合
				if (isCameraRotating)
				{
					previous = current;
					current = new Vector2(this.Mouse.X, this.Mouse.Y);
					Vector2 delta = current - previous;
					delta /= (float)Math.Sqrt(this.Width * this.Width + this.Height * this.Height);
					float length = delta.Length;
					if (length > 0.0)
					{
						float rad = length * MathHelper.Pi;
						float theta = (float)Math.Sin(rad) / length;
						Quaternion after = new Quaternion(delta.Y * theta, delta.X * theta, 0.0f, (float)Math.Cos(rad));
						rotate = rotate * Matrix4.Rotate(after);
					}
				}
			};

			//マウスホイールが回転すると発生するイベント
			this.Mouse.WheelChanged += (sender, e) =>
			{
				float delta = (float)this.Mouse.Wheel - (float)wheelPrevious;

				zoom *= (float)Math.Pow(1.2, delta);

				//拡大、縮小の制限
				if (zoom > 2.0f)
					zoom = 2.0f;
				if (zoom < 0.5f)
					zoom = 0.5f;
				wheelPrevious = this.Mouse.Wheel;
			};

			#endregion

			VSync = VSyncMode.On;
		}

		//ウィンドウの起動時に実行される。
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			GL.ClearColor(Color4.Black);
			GL.Enable(EnableCap.DepthTest);

			Random();

			GL.EnableClientState(ArrayCap.VertexArray);	//VertexArrayを有効化

			//バッファを1コ作成
			GL.GenBuffers(1, out vbo);

			//ArrayBufferとして"VBO"を指定(バインド)
			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

			int size = position.Length * System.Runtime.InteropServices.Marshal.SizeOf(default(Vector3));
			//ArrayBufferにデータをセット
			GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(size), position, BufferUsageHint.StaticDraw);

			//バッファの指定(バインド)を解除
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}

		//ウィンドウの終了時に実行される。
		protected override void OnUnload(EventArgs e)
		{
			base.OnUnload(e);

			GL.DeleteBuffers(1, ref vbo);				//バッファを1コ削除

			GL.DisableClientState(ArrayCap.VertexArray);//VertexArrayを無効化
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

			//Enterキーで点をランダムにする(StaticDrawなので反映はされない)
			if (Keyboard[Key.Enter])
			{
				Random();
			}

			#region Camera__Keyboard

			//F1キーで回転をリセット
			if (Keyboard[Key.F1])
			{
				rotate = Matrix4.Identity;
			}

			//F2キーでY軸90度回転
			if (Keyboard[Key.F2])
			{
				rotate = Matrix4.CreateRotationY(MathHelper.PiOver2);
			}

			//F3キーでY軸180度回転
			if (Keyboard[Key.F3])
			{
				rotate = Matrix4.CreateRotationY(MathHelper.Pi);
			}

			//F4キーでY軸270度回転
			if (Keyboard[Key.F4])
			{
				rotate = Matrix4.CreateRotationY(MathHelper.ThreePiOver2);
			}

			//F5キーで拡大をリセット
			if (Keyboard[Key.F5])
			{
				zoom = 1.0f;
			}

			#endregion
		}

		//画面描画で実行される。
		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			#region TransFormationMatrix

			Matrix4 modelView = Matrix4.LookAt(Vector3.UnitZ * 10 / zoom, Vector3.Zero, Vector3.UnitY);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadMatrix(ref modelView);
			GL.MultMatrix(ref rotate);

			Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4 / zoom, (float)this.Width / (float)this.Height, 1.0f, 64.0f);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref projection);

			#endregion

			GL.Color4(Color4.Red);

			//ArrayBufferとして"VBO"を指定(バインド)
			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

			//頂点のバッファ領域を指定
			GL.VertexPointer(3, VertexPointerType.Float, 0, 0);

			//バッファの内容を直線で描画
			GL.DrawArrays(BeginMode.Lines, 0, position.Length);

			//バッファの指定(バインド)を解除
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			SwapBuffers();
		}

		//頂点の位置をランダムにする
		private void Random()
		{
			Random r = new Random();
			for (int i = 0; i < N; i++)
			{
				position[i].X = (float)r.NextDouble() * 10.0f - 5.0f;
				position[i].Y = (float)r.NextDouble() * 10.0f - 5.0f;
				position[i].Z = (float)r.NextDouble() * 10.0f - 5.0f;
			}
		}
	}
}
