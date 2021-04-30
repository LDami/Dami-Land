using SampSharp.GameMode;
using SampSharpGameMode1.Physics;
using System.Collections;
using System.Collections.Generic;

[assembly: SampSharpExtension(typeof(ColAndreas))]

namespace SampSharpGameMode1.Physics
{
	public partial class ColAndreas : Extension
	{
		public const int InvalidId = 0;

		public static void Init()
		{
			Internal.Init();
		}

		public void RemoveBuilding(int modelid, Vector3 position, float radius)
		{
			if (modelid == InvalidId)
				throw new System.Exception($"Argument modelid is invalid (value: {modelid})");
			Internal.RemoveBuilding(modelid, position.X, position.Y, position.Z, radius);
		}

		public void RestoreBuilding(int modelid, Vector3 position, float radius)
		{
			if (modelid == InvalidId)
				throw new System.Exception($"Argument modelid is invalid (value: {modelid})");
			Internal.RestoreBuilding(modelid, position.X, position.Y, position.Z, radius);
		}

		public RayCastCollisionTarget RayCastLine(Vector3 startPos, Vector3 endPos)
		{
			Internal.RayCastLine(startPos.X, startPos.Y, startPos.Z, endPos.X, endPos.Y, endPos.Z, out float x, out float y, out float z);
			return new Vector3(x, y, z);
		}

		public int RayCastLineID(Vector3 startPos, Vector3 endPos)
		{
			return Internal.RayCastLineID(startPos.X, startPos.Y, startPos.Z, endPos.X, endPos.Y, endPos.Z, out float x, out float y, out float z);
		}

		public int RayCastLineExtraID(int type, float StartX, float StartY, float StartZ, float EndX, float EndY, float EndZ, out float x, out float y, out float z)
		{
			return Internal.RayCastLineExtraID(type, StartX, StartY, StartZ, EndX, EndY, EndZ, out x, out y, out z);
		}

		public RayCastCollisionTarget[] RayCastMultiLine(float StartX, float StartY, float StartZ, float EndX, float EndY, float EndZ, int size)
		{
			Internal.RayCastMultiLine(StartX, StartY, StartZ, EndX, EndY, EndZ, out float[] retx, out float[] rety, out float[] retz, out float[] retdist, out int[] ModelIDs, size);
			RayCastCollisionTarget[] targets = new RayCastCollisionTarget[size];
			for (int i=0; i < size; i++)
			{
				targets[i].position = new Vector3(retx[i], rety[i], retz[i]);
				targets[i].distance = retdist[i];
				targets[i].modelid = ModelIDs[i];
			}
			return targets;
		}

		public int RayCastLineAngle(float StartX, float StartY, float StartZ, float EndX, float EndY, float EndZ, out float x, out float y, out float z, out float rx, out float ry, out float rz)
		{
			return Internal.RayCastLineAngle(StartX, StartY, StartZ, EndX, EndY, EndZ, out x, out y, out z, out rx, out ry, out rz);
		}

		public int RayCastReflectionVector(float StartX, float StartY, float StartZ, float EndX, float EndY, float EndZ, out float x, out float y, out float z, out float nx, out float ny, out float nz)
		{
			return Internal.RayCastLineAngle(StartX, StartY, StartZ, EndX, EndY, EndZ, out x, out y, out z, out nx, out ny, out nz);
		}

		public int RayCastLineNormal(float StartX, float StartY, float StartZ, float EndX, float EndY, float EndZ, out float x, out float y, out float z, out float nx, out float ny, out float nz)
		{
			return Internal.RayCastLineNormal(StartX, StartY, StartZ, EndX, EndY, EndZ, out x, out y, out z, out nx, out ny, out nz);
		}

		public int ContactTest(int modelid, float x, float y, float z, float rx, float ry, float rz)
		{
			if (modelid == InvalidId)
				throw new System.Exception($"Argument modelid is invalid (value: {modelid})");
			return Internal.ContactTest(modelid, x, y, z, rx, ry, rz);
		}

		public void EulerToQuat(float rx, float ry, float rz, out float x, out float y, out float z, out float w)
		{
			Internal.EulerToQuat(rx, ry, rz, out x, out y, out z, out w);
		}

		public void QuatToEuler(float x, float y, float z, float w, out float rx, out float ry, out float rz)
		{
			Internal.QuatToEuler(x, y, z, w, out rx, out ry, out rz);
		}

		public int GetModelBoundingSphere(int modelid, out float offx, out float offy, out float offz, out float radius)
		{
			if (modelid == InvalidId)
				throw new System.Exception($"Argument modelid is invalid (value: {modelid})");
			return Internal.GetModelBoundingSphere(modelid, out offx, out offy, out offz, out radius);
		}

		public int GetModelBoundingBox(int modelid, out float minx, out float miny, out float minz, out float maxx, out float maxy, out float maxz)
		{
			if (modelid == InvalidId)
				throw new System.Exception($"Argument modelid is invalid (value: {modelid})");
			return Internal.GetModelBoundingBox(modelid, out minx, out miny, out minz, out maxx, out maxy, out maxz);
		}

		public void SetObjectExtraID(int index, int type, int data)
		{
			Internal.SetObjectExtraID(index, type, data);
		}

		public int GetObjectExtraID(int index, int type)
		{
			return Internal.GetObjectExtraID(index, type);
		}

		public int RayCastLineEx(float StartX, float StartY, float StartZ, float EndX, float EndY, float EndZ, out float x, out float y, out float z, out float rx, out float ry, out float rz, out float rw, out float cx, out float cy, out float cz)
		{
			return Internal.RayCastLineEx(StartX, StartY, StartZ, EndX, EndY, EndZ, out x, out y, out z, out rx, out ry, out rz, out rw, out cx, out cy, out cz);
		}

		public int RayCastLineAngleEx(float StartX, float StartY, float StartZ, float EndX, float EndY, float EndZ, out float x, out float y, out float z, out float rx, out float ry, out float rz, out float rw, out float ocx, out float ocy, out float ocz, out float orx, out float ory, out float orz)
		{
			return Internal.RayCastLineAngleEx(StartX, StartY, StartZ, EndX, EndY, EndZ, out x, out y, out z, out rx, out ry, out rz, out rw, out ocx, out ocy, out ocz, out orx, out ory, out orz);
		}
	}
}
