using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace OpenTK_Samples
{
	struct Vertex
	{
		public Vector3 position;
		public Vector3 normal;
		public Color4 color;

		public Vertex(Vector3 position, Vector3 normal, Color4 color)
		{
			this.position = position;
			this.normal = normal;
			this.color = color;
		}

		public static readonly int Size = Marshal.SizeOf(default(Vertex));
	}

	//各モデルの骨組み
	abstract class Model
	{
		protected Vertex[] vertices;		//頂点
		protected int[] indices;			//頂点の指標
		protected int vbo;					//VBOのバッファの識別番号を保持
		protected int ibo;					//IBOのバッファの識別番号を保持
		protected int vao;					//VAOの識別番号を保持

		protected Model()
		{
			this.vbo = 0;
			this.ibo = 0;
			this.vao = 0;
		}

		public abstract void Initialize(Color4 color);

		public void CreateBuffers()
		{
			//VBOを1コ生成し、頂点データを送り込む
			GL.GenBuffers(1, out vbo);
			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
			int vertexArray1Size = vertices.Length * Vertex.Size;
			GL.BufferData<Vertex>(BufferTarget.ArrayBuffer, new IntPtr(vertexArray1Size), vertices, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			//IBOを1コ生成し、インデックスデータを送り込む
			GL.GenBuffers(1, out ibo);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
			int indexArray1Size = indices.Length * sizeof(int);
			GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(indexArray1Size), indices, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

			//VAOを1コ作成
			GL.GenVertexArrays(1, out vao);

			GL.BindVertexArray(vao);

			//各Arrayを有効化
			GL.EnableClientState(ArrayCap.VertexArray);
			GL.EnableClientState(ArrayCap.NormalArray);
			GL.EnableClientState(ArrayCap.ColorArray);

			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

			//頂点の位置情報の場所を指定
			GL.VertexPointer(3, VertexPointerType.Float, Vertex.Size, 0);

			//頂点の法線情報の場所を指定
			GL.NormalPointer(NormalPointerType.Float, Vertex.Size, Vector3.SizeInBytes);

			//頂点の色情報の場所を指定
			GL.ColorPointer(4, ColorPointerType.Float, Vertex.Size, Vector3.SizeInBytes * 2);

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			GL.BindVertexArray(0);
		}

		public void DeleteBuffers()
		{
			GL.DeleteBuffers(1, ref vbo);			//バッファを1コ削除
			GL.DeleteBuffers(1, ref ibo);			//バッファを1コ削除
			GL.DeleteVertexArrays(1, ref vao);		//VAOを1コ削除
		}

		public void Draw()
		{
			GL.BindVertexArray(vao);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
			GL.DrawElements(BeginMode.Quads, indices.Length, DrawElementsType.UnsignedInt, 0);
			GL.BindVertexArray(0);
		}
	}

	//トーラスモデルの実装
	class TorusModel : Model
	{
		int row;
		int column;
		double smallRadius;
		double largeRadius;

		public TorusModel(int row, int column, double smallRadius, double largeRadius)
			: base()
		{
			this.row = row;
			this.column = column;
			this.smallRadius = smallRadius;
			this.largeRadius = largeRadius;
		}

		public override void Initialize(Color4 color)
		{
			LinkedList<Vertex> vertexList = new LinkedList<Vertex>();
			LinkedList<int> indexList = new LinkedList<int>();

			for (int i = 0; i <= row; i++)
			{
				double sr = (2.0 * Math.PI / row) * i;
				double cossr = Math.Cos(sr);
				double sinsr = Math.Sin(sr);

				double sx = cossr * smallRadius;
				double sy = sinsr * smallRadius;
				for (int j = 0; j <= column; j++)
				{
					double lr = (2.0 * Math.PI / column) * j;
					double coslr = Math.Cos(lr);
					double sinlr = Math.Sin(lr);

					double px = coslr * (sx + largeRadius);
					double py = sy;
					double pz = sinlr * (sx + largeRadius);

					double nx = cossr * coslr;
					double ny = sinsr;
					double nz = cossr * sinlr;

					Vector3 position = new Vector3((float)px, (float)py, (float)pz);
					Vector3 normal = new Vector3((float)nx, (float)ny, (float)nz);
					vertexList.AddLast(new Vertex(position, normal, color));
				}
			}

			for (int i = 0; i < row; i++)
			{
				for (int j = 0; j < column; j++)
				{
					int d = i * (column + 1) + j;
					indexList.AddLast(d);
					indexList.AddLast(d + column + 1);
					indexList.AddLast(d + column + 2);
					indexList.AddLast(d + 1);
				}
			}

			base.vertices = vertexList.ToArray();
			base.indices = indexList.ToArray();
		}
	}

	//球モデルの実装
	class SphereModel : Model
	{
		int slice;
		int stack;
		float radius;

		public SphereModel(int slice, int stack, float radius)
			: base()
		{
			this.slice = slice;
			this.stack = stack;
			this.radius = radius;
		}

		public override void Initialize(Color4 color)
		{
			LinkedList<Vertex> vertexList = new LinkedList<Vertex>();
			LinkedList<int> indexList = new LinkedList<int>();

			for (int i = 0; i <= stack; i++)
			{
				double p = Math.PI / stack * i;
				double pHeight = Math.Cos(p);
				double pWidth = Math.Sin(p);

				for (int j = 0; j <= slice; j++)
				{
					double rotor = 2 * Math.PI / slice * j;
					double x = Math.Cos(rotor);
					double y = Math.Sin(rotor);

					Vector3 position = new Vector3((float)(radius * x * pWidth), (float)(radius * pHeight), (float)(radius * y * pWidth));
					Vector3 normal = new Vector3((float)(x * pWidth), (float)pHeight, (float)(y * pWidth));
					vertexList.AddLast(new Vertex(position, normal, color));
				}
			}

			for (int i = 0; i <= stack; i++)
			{
				for (int j = 0; j <= slice; j++)
				{
					int d = i * (slice + 1) + j;
					indexList.AddLast(d);
					indexList.AddLast(d + 1);
					indexList.AddLast(d + slice + 2);
					indexList.AddLast(d + slice + 1);
				}
			}
			base.vertices = vertexList.ToArray();
			base.indices = indexList.ToArray();
		}
	}

	//地面モデルの実装
	class PlateModel : Model
	{
		int row;
		int column;
		float size;
		float height;

		public PlateModel(int row, int column, float size, float height)
			: base()
		{
			this.row = row;
			this.column = column;
			this.size = size;
			this.height = height;
		}

		public override void Initialize(Color4 c)
		{
			vertices = new Vertex[(row + 1) * (column + 1)];
			indices = new int[row * column * 4];

			//位置、法線、色
			for (int i = 0; i <= row; i++)
			{
				for (int j = 0; j <= column; j++)
				{
					float x = (4.0f * (float)i / (float)(row + 1) - 2.0f) * size;
					float z = (4.0f * (float)j / (float)(column + 1) - 2.0f) * size;

					Vector3 position = new Vector3(x, height, z);
					Vector3 normal = Vector3.UnitY;
					Color4 color = ((i + j) % 2 == 0) ? c : Color4.White;
					vertices[i * (column + 1) + j] = new Vertex(position, normal, color);
				}
			}

			//インデックス
			for (int i = 0; i < row; i++)
			{
				for (int j = 0; j < column; j++)
				{
					indices[(i * column + j) * 4] = i * (column + 1) + j;
					indices[(i * column + j) * 4 + 1] = i * (column + 1) + j + 1;
					indices[(i * column + j) * 4 + 2] = (i + 1) * (column + 1) + j + 1;
					indices[(i * column + j) * 4 + 3] = (i + 1) * (column + 1) + j;
				}
			}
		}
	}
}
