using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.IO;

/*
 * 参照設定 : OpenTK System System.Drawing
 */

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

		int vertexShader;
		int fragmentShader;
		int shaderProgram;

		//800x600のウィンドウを作る。タイトルは「3-1:Programmable Shader」
		public Game()
			: base(800, 600, GraphicsMode.Default, "3-1:Programmable Shader")
		{
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

			GL.ClearColor(Color4.Gray);

			#region Shader Compile and Link
			int status;

			//シェーダオブジェクト(バーテックス)を生成
			vertexShader = GL.CreateShader(ShaderType.VertexShader);

			using (var sr = new StreamReader("03-01_shader.vert"))
			{
				//バーテックスシェーダのコードを指定
				GL.ShaderSource(vertexShader, sr.ReadToEnd());
			}
			//バーテックスシェーダをコンパイル
			GL.CompileShader(vertexShader);
			GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out status);
			//コンパイル結果をチェック
			if (status == 0)
			{
				throw new ApplicationException(GL.GetShaderInfoLog(vertexShader));
			}

			//シェーダオブジェクト(フラグメント)を生成
			fragmentShader = GL.CreateShader(ShaderType.FragmentShader);

			using (var sr = new StreamReader("03-01_shader.frag"))
			{
				//フラグメントシェーダのコードを指定
				GL.ShaderSource(fragmentShader, sr.ReadToEnd());
			}
			//フラグメントシェーダをコンパイル
			GL.CompileShader(fragmentShader);
			GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out status);
			//コンパイル結果をチェック
			if (status == 0)
			{
				throw new ApplicationException(GL.GetShaderInfoLog(fragmentShader));
			}

			//シェーダプログラムのオブジェクトを生成
			shaderProgram = GL.CreateProgram();

			//各シェーダオブジェクトをシェーダプログラムへ登録
			GL.AttachShader(shaderProgram, vertexShader);
			GL.AttachShader(shaderProgram, fragmentShader);

			//不要になった各シェーダオブジェクトを削除
			GL.DeleteShader(vertexShader);
			GL.DeleteShader(fragmentShader);

			//シェーダプログラムのリンク
			GL.LinkProgram(shaderProgram);
			GL.GetProgram(shaderProgram, ProgramParameter.LinkStatus, out status);
			//シェーダプログラムのリンクのチェック
			if (status == 0)
			{
				throw new ApplicationException(GL.GetProgramInfoLog(shaderProgram));
			}
			#endregion
			
			//シェーダプログラムを使用
			GL.UseProgram(shaderProgram);
		}

		//ウィンドウの終了時に実行される。
		protected override void OnUnload(EventArgs e)
		{
			base.OnUnload(e);

			GL.DeleteProgram(shaderProgram);
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

			GL.Clear(ClearBufferMask.ColorBufferBit);

			#region TransFormationMatrix

			Matrix4 modelView = Matrix4.LookAt(Vector3.UnitZ * 10 / zoom, Vector3.Zero, Vector3.UnitY);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadMatrix(ref modelView);
			GL.MultMatrix(ref rotate);

			Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4 / zoom, (float)this.Width / (float)this.Height, 1.0f, 64.0f);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref projection);

			#endregion

			GL.Begin(BeginMode.Quads);
			GL.Color4(Color.Blue);
			GL.Vertex3(-3.0f, 3.0f, 0.0f);
			GL.Vertex3(-3.0f, -3.0f, 0.0f);
			GL.Vertex3(3.0f, -3.0f, 0.0f);
			GL.Vertex3(3.0f, 3.0f, 0.0f);
			GL.End();

			SwapBuffers();
		}
	}
}
