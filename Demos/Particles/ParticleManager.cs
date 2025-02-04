﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Threading.Tasks;
using System.Graphics.Models;

namespace Particles {
	public class ParticleManager : Scene {
		public const int NumberOfParticles = 15;
		public const int edgeDistance = 150;
		public const float particleProximity = 115f;
		private static Comparison<Point> comparePoints = ComparePoints;
		private Particle[] particles;
		private Action<int> updateParticle;
		internal Point[] concentricPoints;
		private ParticlesDemo parent;

		/// <summary>
		/// Use this for initialization
		/// </summary>
		public ParticleManager(ParticlesDemo parent) {
			LightingEnabled = false;
			this.parent = parent;
			updateParticle = UpdateParticle;
			int x;
			List<Point> points = new List<Point>(1000000);
			for (int y = -parent.heightMinusBorderOffset; y < parent.heightMinusBorderOffset; y++) {
				for (x = -parent.widthMinusBorderOffset; x < parent.widthMinusBorderOffset; x++)
					points.Add(new Point(x, y));
			}
			points.Sort(comparePoints);
			points.RemoveAt(0);
			concentricPoints = points.ToArray();
			particles = new Particle[NumberOfParticles];
			TextureCollection texture = new TextureCollection(new Texture2D(Properties.Resources.Particle, NPotTextureScaleMode.ScaleUp, ImageParameterAction.Dispose, true, false));
			for (int i = 0; i < NumberOfParticles; i++) {
				Particle particle = new Particle(texture, new Vector2(UniformRandom.RandomFloat * 2f - 1f, UniformRandom.RandomFloat * 2f - 1f), new Vector2(0.2f, 0.2f));
				particle.Name = "Particle " + i;
				particles[i] = particle;
				Add(particle);
			}
		}

		private static int ComparePoints(Point left, Point right) {
			return (left.X * left.X + left.Y * left.Y).CompareTo(right.X * right.X + right.Y * right.Y);
		}

		public void Update() {
			for (int i = 0; i < particles.Length; i++)
				particles[i].Update(particles);
			ParallelLoop.For(0, particles.Length, updateParticle);
		}

		private void UpdateParticle(int i) {
			Vector2 pos = particles[i].Location.ToVector2();
			for (int j = 0; j < particles.Length; j++) {
				if (i != j)
					pos = Approach(pos, particles[j].Location.ToVector2(), 1.3f, 0.01f, 1f, 0.01f);
			}
			Point? webcamCoordinate = GetClosestEdge((int) pos.X, (int) pos.Y);
			if (webcamCoordinate != null)
				pos = Approach(pos, webcamCoordinate.Value.ToVector2(), 0.1f, 0.5f, 0.1f, 0.1f);
			particles[i].Location = new Vector3(Vector2.Clamp(pos + ((pos - particles[i].Location.ToVector2()) * 0.2f), new Vector2(-1, -1), new Vector2(1, 1)), 0f);
		}

		public Point? GetClosestEdge(int x, int y) {
			int currentX, currentY, i;
			Vector2 vector;
			bool notInCloseProximity;
			int lastEdgeIndex = -1;
			for (int pointIndex = 0; pointIndex < concentricPoints.Length; pointIndex++) {
				currentX = x + concentricPoints[pointIndex].X;
				currentY = y + concentricPoints[pointIndex].Y;
				if (currentY >= ParticlesDemo.borderOffset && currentY < parent.heightMinusBorderOffset && currentX >= ParticlesDemo.borderOffset && currentX < parent.widthMinusBorderOffset && parent.isEdge[currentX][currentY] && !parent.ignore[currentX][currentY]) {
					notInCloseProximity = true;
					vector = new Vector2(currentX, currentY);
					for (i = 0; i < particles.Length; i++) {
						if ((particles[i].Location.ToVector2() - vector).Length() < particleProximity) {
							notInCloseProximity = false;
							break;
						}
					}
					if (notInCloseProximity) {
						if (lastEdgeIndex == -1 || pointIndex - lastEdgeIndex < edgeDistance)
							lastEdgeIndex = pointIndex;
						else
							return new Point(currentX, currentY);
					}
				}
			}
			return null;
		}

		public void Render2D(Graphics g, Size canvasSize) {
			for (int i = 0; i < particles.Length; i++)
				particles[i].Render2D(g, canvasSize);
		}

		/// <summary>
		/// Keeps a certain specified the distance from the specified point for a certain object.
		/// </summary>
		/// <param name="currentPos">The current position of the particle.</param>
		/// <param name="flockPoint">The point the particle should flock toward at a certain distance.</param>
		/// <param name="distance">The distance to keep from the flock point.</param>
		/// <param name="attractSpeed">The speed the particles should move towards the flock point at.</param>
		/// <param name="repelSpeed">The speed the particles should move away from the flock point at.</param>
		public static Vector2 Approach(Vector2 currentPos, Vector2 flockPoint, float distance, float attractSpeed, float repelSpeed) {
			Vector2 distVector = flockPoint - currentPos;
			float targetDistance = distVector.Length() - distance;
			Vector2 resultantDist = Vector2.Normalize(distVector.LengthSquared() <= float.Epsilon ? new Vector2(UniformRandom.RandomFloat, UniformRandom.RandomFloat) : distVector);
			return currentPos + resultantDist * ((targetDistance < 0f ? repelSpeed : attractSpeed) * targetDistance);
		}

		/// <summary>
		/// Keeps a certain specified the distance from the specified point for a certain object.
		/// </summary>
		/// <param name="currentPos">The current position of the particle.</param>
		/// <param name="flockPoint">The point the particle should flock toward at a certain distance.</param>
		/// <param name="distance">The distance to keep from the flock point.</param>
		/// <param name="attractSpeed">The speed the particles should move towards the flock point at.</param>
		/// <param name="repelSpeed">The speed the particles should move away from the flock point at.</param>
		/// <param name="kineticEnergy">The minimum movement of the particle.</param>
		public static Vector2 Approach(Vector2 currentPos, Vector2 flockPoint, float distance, float attractSpeed, float repelSpeed, float kineticEnergy) {
			Vector2 distVector = flockPoint - currentPos;
			float targetDistance = distVector.Length() - distance;
			Vector2 normal = new Vector2(-distVector.Y, distVector.X);
			Vector2 resultantDist;
			if (distVector.LengthSquared() <= float.Epsilon)
				resultantDist = Vector2.Normalize(new Vector2(UniformRandom.RandomFloat, UniformRandom.RandomFloat) * repelSpeed);
			else {
				float remainingEnergy = targetDistance == 0f ? Math.Sign(targetDistance - kineticEnergy) : (kineticEnergy / targetDistance) * Math.Sign(targetDistance - kineticEnergy);
				resultantDist = Vector2.Normalize(distVector + normal * remainingEnergy);
			}
			return currentPos + resultantDist * ((targetDistance < 0f ? repelSpeed : attractSpeed) * targetDistance);
		}
	}
}