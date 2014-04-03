﻿using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

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

		int texture;

		//800x600のウィンドウを作る。タイトルは「2-1:Show Texture」
		public Game()
			: base(800, 600, GraphicsMode.Default, "2-1:Show Texture")
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

			GL.Enable(EnableCap.Texture2D);

			//テクスチャ用バッファの生成
			texture = GL.GenTexture();

			//テクスチャ用バッファのひもづけ
			GL.BindTexture(TextureTarget.Texture2D, texture);

			//テクスチャの設定
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

			//テクスチャの色情報を作成
			int size = 8;
			float[, ,] colors = new float[size, size, 4];
			for (int i = 0; i < colors.GetLength(0); i++)
			{
				for (int j = 0; j < colors.GetLength(1); j++)
				{
					colors[i, j, 0] = (float)i / size;
					colors[i, j, 1] = (float)j / size;
					colors[i, j, 2] = 0.0f;
					colors[i, j, 3] = 1.0f;
				}
			}

			//テクスチャ用バッファに色情報を流し込む
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, size, size, 0, PixelFormat.Rgba, PixelType.Float, colors);
		}

		//ウィンドウの終了時に実行される。
		protected override void OnUnload(EventArgs e)
		{
			base.OnUnload(e);

			//使用したテクスチャを削除
			GL.DeleteTexture(texture);
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

			//ポリゴン表示
			GL.Color4(Color4.White);
			
			GL.Begin(BeginMode.Quads);

			GL.TexCoord2(1.0, 1.0);
			GL.Vertex3(1, 1, 0);

			GL.TexCoord2(0.0, 1.0);
			GL.Vertex3(-1, 1, 0);

			GL.TexCoord2(0.0, 0.0);
			GL.Vertex3(-1, -1, 0);

			GL.TexCoord2(1.0, 0.0);
			GL.Vertex3(1, -1, 0);

			GL.End();


			#region テクスチャの座標説明コード
			//GL.Begin(BeginMode.Quads);
			/*
			//はみ出し
			GL.TexCoord2(1.5, 1.5);
			GL.Vertex3(1, 1, 0);
			GL.TexCoord2(-0.5, 1.5);
			GL.Vertex3(-1, 1, 0);
			GL.TexCoord2(-0.5, -0.5);
			GL.Vertex3(-1, -1, 0);
			GL.TexCoord2(1.5, -0.5);
			GL.Vertex3(1, -1, 0);
			
			//0.25～0.75四方
			GL.TexCoord2(0.75, 0.75);
			GL.Vertex3(1, 1, 0);
			GL.TexCoord2(0.25, 0.75);
			GL.Vertex3(-1, 1, 0);
			GL.TexCoord2(0.25, 0.25);
			GL.Vertex3(-1, -1, 0);
			GL.TexCoord2(0.75, 0.25);
			GL.Vertex3(1, -1, 0);
			*/
			/*
			//複数ポリゴンに適応
			GL.TexCoord2(1.0, 1.0);
			GL.Vertex3(1, 1, 0);
			GL.TexCoord2(0.5, 1.0);
			GL.Vertex3(0, 0, 0);
			GL.TexCoord2(0.5, 0.0);
			GL.Vertex3(0, -1, 0);
			GL.TexCoord2(1.0, 0.0);
			GL.Vertex3(1, 0, 0);
			GL.TexCoord2(0.5, 1.0);
			GL.Vertex3(0, 0, 0);
			GL.TexCoord2(0.0, 1.0);
			GL.Vertex3(-1, 1, 0);
			GL.TexCoord2(0.0, 0.0);
			GL.Vertex3(-1, 0, 0);
			GL.TexCoord2(0.5, 0.0);
			GL.Vertex3(0, -1, 0);
			*/
			/*
			//歪み
			GL.TexCoord2(1.0, 1.0);
			GL.Vertex3(1, 1, 0);
			GL.TexCoord2(0.0, 0.25);
			GL.Vertex3(-1, 1, 0);
			GL.TexCoord2(0.75, 0.5);
			GL.Vertex3(-1, -1, 0);
			GL.TexCoord2(1.0, 0.75);
			GL.Vertex3(1, -1, 0);
			*/
			//GL.End();
			#endregion

			SwapBuffers();
		}
	}
}