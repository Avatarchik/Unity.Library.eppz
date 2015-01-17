﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace EPPZGeometry
{


	public class Segment
	{


		static public float defaultAccuracy = 1e-6f;

		public enum ContainmentMethod { Default, Precise }
		static public ContainmentMethod defaultContainmentMethod = ContainmentMethod.Default;

		private Vector2 _a;
		public virtual Vector2 a
		{ 
			get { return _a; }
			set { _a = value; }
		}
		private Vector2 _b;
		public virtual Vector2 b
		{ 
			get { return _b; }
			set { _b = value; }
		}

		
		/*
		 * 
		 * Factories
		 * 
		 */
		
		public static Segment SegmentWithSource(EPPZGeometry_SegmentSource segmentSource)
		{
			return Segment.SegmentWithPointTransforms(segmentSource.pointTransforms);
		}
		
		public static Segment SegmentWithPointTransforms(Transform[] pointTransforms) // Uses Transform.localPosition.xy()
		{
			return Segment.SegmentWithPoints(pointTransforms[0].position, pointTransforms[1].position);
		}

		public static Segment SegmentWithPoints(Vector2 a_, Vector2 b_)
		{
			Segment instance = new Segment();
			instance.a = a_;
			instance.b = b_;
			return instance;
		}

		
		/*
		 * 
		 * Model updates
		 * 
		 */ 
		
		public void UpdateWithSource(EPPZGeometry_SegmentSource segmentSource) // Assuming unchanged point count
		{
			UpdateWithTransforms(segmentSource.pointTransforms);
		}
		
		public void UpdateWithTransforms(Transform[] pointTransforms) // Assuming unchanged point count
		{
			a = pointTransforms[0].position;
			b = pointTransforms[1].position;
		}
		
		
		/*
		 * 
		 * Accessors
		 * 
		 */ 

		public Rect bounds // Readonly
		{
			get
			{
				Rect bounds_ = new Rect();

				// Set bounds.
				bounds_.xMin = Mathf.Min(a.x, b.x);
				bounds_.xMax = Mathf.Max(a.x, b.x);
				bounds_.yMin = Mathf.Min(a.y, b.y);
				bounds_.yMax = Mathf.Max(a.y, b.y);

				return bounds_;
			}
		}

		/**
		 * True when the two segments are intersecting. Not true when endpoints
		 * are equal, nor when a point is contained by other segment.
		 */
		public bool IsIntersectingWithSegment(Segment segment)
		{
			// No intersecting if bounds don't even overlap (slight optimization).
			bool boundsOverlaps = this.bounds.Overlaps(segment.bounds);
			if (boundsOverlaps == false) return false;

			// Do the Bryce Boe test.
			return Geometry.AreSegmentsIntersecting(this.a, this.b, segment.a, segment.b);
		}
		
		public bool ContainsPoint(Vector2 point)
		{ return ContainsPoint(point, defaultAccuracy); }

		public bool ContainsPoint(Vector2 point, float accuracy)
		{ return ContainsPoint(point, accuracy, ContainmentMethod.Default); }

		public bool IsPointLeft(Vector2 point)
		{ return Geometry.PointIsLeftOfSegment(point, this.a, this.b);  }

		public bool ContainsPoint(Vector2 point, float accuracy, ContainmentMethod containmentMethod)
		{			
			float treshold = accuracy / 2.0f;

			// Expanded bounds containment test.
			Rect bounds_ = this.bounds;
			Rect expandedBounds = Rect.MinMaxRect(
				bounds_.xMin - treshold,
				bounds_.yMin - treshold,
				bounds_.xMax + treshold,
				bounds_.yMax + treshold);
			bool expandedBoundsContainment = expandedBounds.Contains(point);
			if (expandedBoundsContainment == false) return false; // Only if passed

			// Line distance test.
			float distance = this.DistanceToPoint(point);
			bool lineDistanceTest = distance < treshold;
			if (lineDistanceTest == false) return false; // Only if passed

			if (containmentMethod == ContainmentMethod.Precise)
			{
				// Perpendicular segment.
				Vector2 normalizedHalf = (this.b - this.a) / 2.0f;
				float halfLength = normalizedHalf.magnitude;
				Vector2 normalizedHalfPerpendicular = new Vector2( -normalizedHalf.y, normalizedHalf.x );
				Vector2 perpendicular_a = this.a + normalizedHalf;
				Vector2 perpendicular_b = this.a + normalizedHalf + normalizedHalfPerpendicular;

				// Perpendicular line distance test.
				float perpendicularDistance = Geometry.PointDistanceFromLine(point, perpendicular_a, perpendicular_b);
				bool perpendicularDistanceTest = perpendicularDistance < halfLength;

				// Endpoint distance test if previous failed.
				if (perpendicularDistanceTest == false)
				{
					float distanceFromEndPoints = Mathf.Min( Vector2.Distance(this.a, point), Vector2.Distance(this.b, point) );
					bool endpointDistanceTest = distanceFromEndPoints < treshold;
					if (endpointDistanceTest == false) return false; // Only if passed
				}
			}

			// All passed.
			return true;
		}

		public float DistanceToPoint(Vector2 point_)
		{
			return Geometry.PointDistanceFromLine(point_, this.a, this.b);
		}
	}
}