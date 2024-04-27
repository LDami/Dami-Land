using SampSharp.GameMode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampSharpGameMode1
{
    internal class QuaternionHelper
    {
        public static float CalculateRotationAngle(Vector2 pointA, Vector2 pointB, bool isZAngle = false)
        {
            // Calculer la différence entre les coordonnées de B et A
            Vector2 direction = pointB - pointA;

            // Utiliser la fonction Atan2 pour calculer l'angle en radians
            double angleRadians = Math.Atan2(direction.Y, direction.X);

            // Convertir l'angle en degrés
            double angleDegrees = Math.Abs(angleRadians) * (180.0f / Math.PI);

            if (isZAngle)
                angleDegrees = 270f - angleDegrees;
            // Ajuster l'angle pour qu'il soit dans la plage [0, 360]
            angleDegrees = (angleDegrees + 360) % 360;

            return (float)angleDegrees;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="angle">Angle in Radians</param>
        /// <returns></returns>
        public static Quaternion FromAngle(Vector3 vec)
        {
            // This function is based on Open.MP implementation:
            // https://github.com/openmultiplayer/open.mp/blob/833af233c2e297524f1cd4177ee5d9f306403a67/SDK/include/gtaquat.hpp
            Vector3 c = new(Math.Cos(vec.X * -0.5f), Math.Cos(vec.Y * -0.5f), Math.Cos(vec.Z * -0.5f));
            Vector3 s = new(Math.Sin(vec.X * -0.5f), Math.Sin(vec.Y * -0.5f), Math.Sin(vec.Z * -0.5f));

            return new Quaternion(
                w: c.X * c.Y * c.Z + s.X * s.Y * s.Z,
                x: c.X * s.Y * s.Z + s.X * c.Y * c.Z,
                y: c.X * s.Y * c.Z - s.X * c.Y * s.Z,
                z: c.X * c.Y * s.Z - s.X * s.Y * c.Z
            );
        }

        public static Quaternion FromEuler(float x, float y, float z)
        {
            // Convertir les angles d'Euler en radians
            float angleX = x * (float)(Math.PI / 180.0);
            float angleY = y * (float)(Math.PI / 180.0);
            float angleZ = z * (float)(Math.PI / 180.0);

            // Calculer les moitiés des angles
            float halfX = 0.5f * angleX;
            float halfY = 0.5f * angleY;
            float halfZ = 0.5f * angleZ;

            // Calculer les fonctions trigonométriques
            float cosHalfX = (float)Math.Cos(halfX);
            float cosHalfY = (float)Math.Cos(halfY);
            float cosHalfZ = (float)Math.Cos(halfZ);
            float sinHalfX = (float)Math.Sin(halfX);
            float sinHalfY = (float)Math.Sin(halfY);
            float sinHalfZ = (float)Math.Sin(halfZ);

            // Créer le quaternion
            Quaternion result = new Quaternion(
                sinHalfX * cosHalfY * cosHalfZ - cosHalfX * sinHalfY * sinHalfZ,
                cosHalfX * sinHalfY * cosHalfZ + sinHalfX * cosHalfY * sinHalfZ,
                cosHalfX * cosHalfY * sinHalfZ - sinHalfX * sinHalfY * cosHalfZ,
                cosHalfX * cosHalfY * cosHalfZ + sinHalfX * sinHalfY * sinHalfZ
            );

            return result;
        }
        public static Vector3 ToEuler(float w, float x, float y, float z)
        {
            // Convertir le quaternion en angles d'Euler
            float sinr_cosp = 2 * (w * x + y * z);
            float cosr_cosp = 1 - 2 * (x * x + y * y);
            float roll = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            float sinp = 2 * (w * y - z * x);
            float pitch;
            if (Math.Abs(sinp) >= 1)
                pitch = (float)Math.CopySign(Math.PI / 2, sinp); // Utiliser 90 degrés si l'inclinaison est proche de 90 degrés
            else
                pitch = (float)Math.Asin(sinp);

            float siny_cosp = 2 * (w * z + x * y);
            float cosy_cosp = 1 - 2 * (y * y + z * z);
            float yaw = (float)Math.Atan2(siny_cosp, cosy_cosp);

            // Convertir les angles d'Euler en degrés
            float rollDeg = roll * (180.0f / (float)Math.PI);
            float pitchDeg = pitch * (180.0f / (float)Math.PI);
            float yawDeg = yaw * (180.0f / (float)Math.PI);

            return new Vector3(rollDeg, pitchDeg, yawDeg);
        }

        public static Quaternion LookRotationToPoint(Vector3 pointA, Vector3 pointB)
        {
            // Calculer la rotation autour de l'axe X
            float angleX = CalculateRotationAngle(new Vector2(pointA.Z, pointA.Y), new Vector2(pointB.Z, pointB.Y));
            angleX = 0;
            Quaternion rotationX = FromEuler(angleX, 0, 0);

            // Calculer la rotation autour de l'axe Y
            float angleY = CalculateRotationAngle(new Vector2(pointA.X, pointA.Z), new Vector2(pointB.X, pointB.Z));
            Quaternion rotationY = FromEuler(0, angleY, 0);

            // Calculer la rotation autour de l'axe Z (en supposant que Z est l'axe de hauteur)
            float angleZ = CalculateRotationAngle(new Vector2(pointA.X, pointA.Y), new Vector2(pointB.X, pointB.Y), true);
            Quaternion rotationZ = FromEuler(0, 0, angleZ);

            Console.WriteLine($"LookRotationToPoint: x = {angleX}, y = {angleY}, z = {angleZ}, quatZ: {rotationZ.ToVector4()}");

            // Combiner les rotations pour obtenir le quaternion final
            Quaternion finalRotation = rotationX * rotationY * rotationZ;
            finalRotation = FromEuler(angleX, angleY, angleZ);
            return finalRotation;
        }
    }
}
