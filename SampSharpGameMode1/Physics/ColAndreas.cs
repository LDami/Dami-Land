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

		public static void RemoveBuilding(int modelid, Vector3 position, float radius)
		{
			if (modelid == InvalidId)
				throw new System.Exception($"Argument modelid is invalid (value: {modelid})");
			Internal.RemoveBuilding(modelid, position.X, position.Y, position.Z, radius);
		}
		
		public static void RestoreBuilding(int modelid, Vector3 position, float radius)
		{
			if (modelid == InvalidId)
				throw new System.Exception($"Argument modelid is invalid (value: {modelid})");
			Internal.RestoreBuilding(modelid, position.X, position.Y, position.Z, radius);
		}

		public static RayCastCollisionTarget RayCastLine(Vector3 startPos, Vector3 endPos)
		{
			int modelid = Internal.RayCastLine(startPos.X, startPos.Y, startPos.Z, endPos.X, endPos.Y, endPos.Z, out float x, out float y, out float z);
			return new RayCastCollisionTarget(new Vector3(x, y, z), Vector3.Distance(new Vector3(x, y, z), startPos), modelid);
		}

		public static int RayCastLineID(Vector3 startPos, Vector3 endPos)
		{
			return Internal.RayCastLineID(startPos.X, startPos.Y, startPos.Z, endPos.X, endPos.Y, endPos.Z, out float x, out float y, out float z);
		}
		/*
		public RayCastCollisionTarget[] RayCastMultiLine(Vector3 startPos, Vector3 endPos, int size)
		{
			float[] retx = new float[size];
			float[] rety = new float[size];
			float[] retz = new float[size];
			float[] retdist = new float[size];
			int[] ModelIDs = new int[size];
			Internal.RayCastMultiLine(startPos.X, startPos.Y, startPos.Z, endPos.X, endPos.Y, endPos.Z, out retx, out rety, out retz, out retdist, out ModelIDs, size);
			RayCastCollisionTarget[] targets = new RayCastCollisionTarget[size];
			for (int i=0; i < size; i++)
			{
				targets[i].position = new Vector3(retx[i], rety[i], retz[i]);
				targets[i].distance = retdist[i];
				targets[i].modelid = ModelIDs[i];
			}
			return targets;
		}
		public int RayCastLineAngle(Vector3 startPos, Vector3 endPos, out float x, out float y, out float z, out float rx, out float ry, out float rz)
		{
			return Internal.RayCastLineAngle(startPos.X, startPos.Y, startPos.Z, endPos.X, endPos.Y, endPos.Z, out x, out y, out z, out rx, out ry, out rz);
		}

		public int RayCastReflectionVector(Vector3 startPos, Vector3 endPos, out float x, out float y, out float z, out float nx, out float ny, out float nz)
		{
			return Internal.RayCastLineAngle(startPos.X, startPos.Y, startPos.Z, endPos.X, endPos.Y, endPos.Z, out x, out y, out z, out nx, out ny, out nz);
		}

		public int RayCastLineNormal(Vector3 startPos, Vector3 endPos, out float x, out float y, out float z, out float nx, out float ny, out float nz)
		{
			return Internal.RayCastLineNormal(startPos.X, startPos.Y, startPos.Z, endPos.X, endPos.Y, endPos.Z, out x, out y, out z, out nx, out ny, out nz);
		}

		public bool ContactTest(int modelid, float x, float y, float z, float rx, float ry, float rz)
		{
			if (modelid == InvalidId)
				throw new System.Exception($"Argument modelid is invalid (value: {modelid})");
			return (Internal.ContactTest(modelid, x, y, z, rx, ry, rz) == 1);
		}
		*/
		public void EulerToQuat(float rx, float ry, float rz, out float x, out float y, out float z, out float w)
		{
			Internal.EulerToQuat(rx, ry, rz, out x, out y, out z, out w);
		}

		public static Vector3 QuatToEuler(Quaternion quaternion)
		{
			Internal.QuatToEuler(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W, out float rx, out float ry, out float rz);
			return new Vector3(rx, ry, rz);
		}
		/*
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
		*/

		public static int FindZ_For2DCoord(float x, float y, out float z)
		{
			return Internal.RayCastLine(x, y, 700.0f, x, y, -1000.0f, out x, out y, out z);
		}
	}
}
