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

        public static Quaternion FromAngle(float angle)
        {
            float c1; float c2; float c3;
            float s1; float s2; float s3;
            c1 = 1.0f;
            c2 = (float)Math.Cos(angle / 2);
            c3 = 1.0f;
            s1 = 0.0f;
            s2 = (float)Math.Sin(angle / 2);
            s3 = 0.0f;
            return new Quaternion(
                w: (c1 * c2 * c3) - (s1 * s2 * s3),
                x: (s1 * s2 * c3) + (c1 * c2 * s3),
                y: (s1 * c2 * c3) + (c1 * s2 * s3),
                z: (c1 * s2 * c3) - (s1 * c2 * s3)
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
