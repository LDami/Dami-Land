using SampSharp.Core.Natives.NativeObjects;

namespace SampSharpGameMode1.Physics
{
	public partial class ColAndreas
	{
		protected static ColAndreasInternal Internal;

		static ColAndreas()
		{
			Internal = NativeObjectProxyFactory.CreateInstance<ColAndreasInternal>();
		}

		public class ColAndreasInternal
		{
			[NativeMethod(Function = "CA_Init")]
			public virtual int Init()
			{
				throw new NativeNotImplementedException();
			}

			[NativeMethod(Function = "CA_RemoveBuilding")]
			public virtual int RemoveBuilding(int modelid, float x, float y, float z, float radius)
			{
				throw new NativeNotImplementedException();
			}

			[NativeMethod(Function = "CA_RestoreBuilding")]
			public virtual int RestoreBuilding(int modelid, float x, float y, float z, float radius)
			{
				throw new NativeNotImplementedException();
			}

			[NativeMethod(Function = "CA_RayCastLine")]
			public virtual int RayCastLine(float StartX, float StartY, float StartZ, float EndX, float EndY, float EndZ, out float x, out float y, out float z)
			{
				throw new NativeNotImplementedException();
			}

			[NativeMethod(Function = "CA_RayCastLineID")]
			public virtual int RayCastLineID(float StartX, float StartY, float StartZ, float EndX, float EndY, float EndZ, out float x, out float y, out float z)
			{
				throw new NativeNotImplementedException();
			}
			/*
			[NativeMethod(Function = "CA_RayCastLineExtraID")]
			public virtual int RayCastLineExtraID(int type, float StartX, float StartY, float StartZ, float EndX, float EndY, float EndZ, out float x, out float y, out float z)
			{
				throw new NativeNotImplementedException();
			}

			[NativeMethod(Function = "CA_RayCastMultiLine")]
			public virtual int RayCastMultiLine(float StartX, float StartY, float StartZ, float EndX, float EndY, float EndZ, out float[] retx, out float[] rety, out float[] retz, out float[] retdist, out int[] ModelIDs, int size)
			{
				throw new NativeNotImplementedException();
			}
			*/

			[NativeMethod(Function = "CA_RayCastLineAngle")]
			public virtual int RayCastLineAngle(float StartX, float StartY, float StartZ, float EndX, float EndY, float EndZ, out float x, out float y, out float z, out float rx, out float ry, out float rz)
			{
				throw new NativeNotImplementedException();
			}

			[NativeMethod(Function = "CA_RayCastReflectionVector")]
			public virtual int RayCastReflectionVector(float StartX, float StartY, float StartZ, float EndX, float EndY, float EndZ, out float x, out float y, out float z, out float nx, out float ny, out float nz)
			{
				throw new NativeNotImplementedException();
			}

			[NativeMethod(Function = "CA_RayCastLineNormal")]
			public virtual int RayCastLineNormal(float StartX, float StartY, float StartZ, float EndX, float EndY, float EndZ, out float x, out float y, out float z, out float nx, out float ny, out float nz)
			{
				throw new NativeNotImplementedException();
			}

			[NativeMethod(Function = "CA_ContactTest")]
			public virtual int ContactTest(int modelid, float x, float y, float z, float rx, float ry, float rz)
			{
				throw new NativeNotImplementedException();
			}

			[NativeMethod(Function = "CA_EulerToQuat")]
			public virtual int EulerToQuat(float rx, float ry, float rz, out float x, out float y, out float z, out float w)
			{
				throw new NativeNotImplementedException();
			}

			[NativeMethod(Function = "CA_QuatToEuler")]
			public virtual int QuatToEuler(float x, float y, float z, float w, out float rx, out float ry, out float rz)
			{
				throw new NativeNotImplementedException();
			}

			[NativeMethod(Function = "CA_GetModelBoundingSphere")]
			public virtual int GetModelBoundingSphere(int modelid, out float offx, out float offy, out float offz, out float radius)
			{
				throw new NativeNotImplementedException();
			}

			[NativeMethod(Function = "CA_GetModelBoundingBox")]
			public virtual int GetModelBoundingBox(int modelid, out float minx, out float miny, out float minz, out float maxx, out float maxy, out float maxz)
			{
				throw new NativeNotImplementedException();
			}

			[NativeMethod(Function = "CA_SetObjectExtraID")]
			public virtual int SetObjectExtraID(int index, int type, int data)
			{
				throw new NativeNotImplementedException();
			}

			[NativeMethod(Function = "CA_GetObjectExtraID")]
			public virtual int GetObjectExtraID(int index, int type)
			{
				throw new NativeNotImplementedException();
			}

			[NativeMethod(Function = "CA_RayCastLineEx")]
			public virtual int RayCastLineEx(float StartX, float StartY, float StartZ, float EndX, float EndY, float EndZ, out float x, out float y, out float z, out float rx, out float ry, out float rz, out float rw, out float cx, out float cy, out float cz)
			{
				throw new NativeNotImplementedException();
			}

			[NativeMethod(Function = "CA_RayCastLineAngleEx")]
			public virtual int RayCastLineAngleEx(float StartX, float StartY, float StartZ, float EndX, float EndY, float EndZ, out float x, out float y, out float z, out float rx, out float ry, out float rz, out float rw, out float ocx, out float ocy, out float ocz, out float orx, out float ory, out float orz)
			{
				throw new NativeNotImplementedException();
			}
			
		}
	}
}
