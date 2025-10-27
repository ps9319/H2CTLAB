using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Seb.Fluid.Simulation
{

	public class Spawner3D : MonoBehaviour
	{
		public int particleSpawnDensity = 600;
		public float3 initialVel;
		public float jitterStrength;
		public bool showSpawnBounds;
		public SpawnRegion[] spawnRegions;

		[Header("Debug Info")] public int debug_num_particles;
		public float debug_spawn_volume;


		public SpawnData GetSpawnData()
		{
			List<float3> allPoints = new();
			List<float3> allVelocities = new();

			foreach (SpawnRegion region in spawnRegions)
			{
				int particlesPerAxis = region.CalculateParticleCountPerAxis(particleSpawnDensity);
				// 스케일 반영
				(float3[] points, float3[] velocities) = SpawnCube(
					particlesPerAxis,
					transform.position + region.centre,
					Vector3.Scale(Vector3.one * region.size, region.scale)
				);
				allPoints.AddRange(points);
				allVelocities.AddRange(velocities);
			}

			return new SpawnData() { points = allPoints.ToArray(), velocities = allVelocities.ToArray() };
		}

		(float3[] p, float3[] v) SpawnCube(int numPerAxis, Vector3 centre, Vector3 size)
		{
			int numPoints = numPerAxis * numPerAxis * numPerAxis;
			float3[] points = new float3[numPoints];
			float3[] velocities = new float3[numPoints];

			int i = 0;

			for (int x = 0; x < numPerAxis; x++)
			{
				for (int y = 0; y < numPerAxis; y++)
				{
					for (int z = 0; z < numPerAxis; z++)
					{
						float tx = x / (numPerAxis - 1f);
						float ty = y / (numPerAxis - 1f);
						float tz = z / (numPerAxis - 1f);

						float px = (tx - 0.5f) * size.x + centre.x;
						float py = (ty - 0.5f) * size.y + centre.y;
						float pz = (tz - 0.5f) * size.z + centre.z;
						float3 jitter = UnityEngine.Random.insideUnitSphere * jitterStrength;
						points[i] = new float3(px, py, pz) + jitter;
						velocities[i] = initialVel;
						i++;
					}
				}
			}

			return (points, velocities);
		}



		void OnValidate()
		{
			debug_spawn_volume = 0;
			debug_num_particles = 0;

			if (spawnRegions != null)
			{
				for (int i = 0; i < spawnRegions.Length; i++)
				{
					// scale 기본값 1,1,1 적용
					if (spawnRegions[i].scale == Vector3.zero)
					{
						spawnRegions[i].scale = Vector3.one;
					}

					debug_spawn_volume += spawnRegions[i].Volume;
					int numPerAxis = spawnRegions[i].CalculateParticleCountPerAxis(particleSpawnDensity);
					debug_num_particles += numPerAxis * numPerAxis * numPerAxis;
				}
			}
		}

		void OnDrawGizmos()
		{
			if (showSpawnBounds && !Application.isPlaying)
			{
				foreach (SpawnRegion region in spawnRegions)
				{
					Gizmos.color = region.debugDisplayCol;
					// 스케일 반영
					Gizmos.DrawWireCube(transform.position + region.centre, Vector3.Scale(Vector3.one * region.size, region.scale));
				}
			}
		}

		[System.Serializable]
		public struct SpawnRegion
		{
			public Vector3 centre;
			public Vector3 scale; // 추가: 개별 스케일
			public float size;
			public Color debugDisplayCol;

			public float Volume => size * size * size * scale.x * scale.y * scale.z; // 스케일 반영

			public int CalculateParticleCountPerAxis(int particleDensity)
			{
				int targetParticleCount = (int)(Volume * particleDensity);
				int particlesPerAxis = (int)Math.Cbrt(targetParticleCount);
				return particlesPerAxis;
			}
		}

		public struct SpawnData
		{
			public float3[] points;
			public float3[] velocities;
		}
	}
}