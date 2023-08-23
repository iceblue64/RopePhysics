/*****************************************************************//**
 * \file   Rope.cs
 * \brief  Behavior of a dynamic rope system connecting a player and a partner object.
 *         It exhibits realistic physics, simulating gravity and applying constraints 
 *         to maintain tension and accurate movement. It dynamically adjusts its segments 
 *         to match the distance between the player and the partner for a believable rope effect.
 * 
 * \authors Emmy Berg
 *          Mike Doeren
 * \date    August 2023
 *********************************************************************/

using System.Collections.Generic;
using UnityEngine;
public class Rope : MonoBehaviour
{
    ////////////////////////////////////////////////////////////////////////
    // VARIABLES ===========================================================

    // Struct representing one segment of the rope
    public struct RopeSegment
    {
        public Vector2 posNow;
        public Vector2 posOld;
        public RopeSegment(Vector2 pos)
        {
            posNow = pos;
            posOld = pos;
        }
    }

    // Number of segments = total distance / Segment size
    float distance;

    private LineRenderer lineRenderer;
    private List<RopeSegment> ropeSegments = new List<RopeSegment>();
    [HideInInspector] public float segmentLength = 0.25f;
    [HideInInspector] public int currRopeSize = 0;
    public int maxRopeSize = 28;

    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] float gravityScale = 5.0f;

    ////////////////////////////////////////////////////////////////////////
    // START ===============================================================

    void Start()
    {
        if (Info.partner == null)
            return;

        // Get the line renderer
        lineRenderer = GetComponent<LineRenderer>();
        Vector3 ropeStartPoint = transform.position; // Start at player position

        // Calculate the number of segments
        distance = Vector2.Distance(Info.player.transform.position, Info.partner.transform.position);
        currRopeSize = (int)(distance / segmentLength);

        // Create and add segments to list
        for (int i = 0; i < maxRopeSize; ++i)
        {
            ropeSegments.Add(new RopeSegment(ropeStartPoint));
            ropeStartPoint.y -= segmentLength; // Avoid overlap
        }
    }

    ////////////////////////////////////////////////////////////////////////
    // FIXED UPDATE ========================================================
    private void FixedUpdate()
    {
        if (Info.partner == null)
            return;

        // Update then draw — order matters!
        Simulate();
        DrawRope();
    }

    ////////////////////////////////////////////////////////////////////////
    // SIMULATE ============================================================

    // Simulate physics/gravity for each rope segment
    private void Simulate()
    {
        // Define the gravitational force applied to each segment
        Vector2 forceGravity = new Vector2(0.0f, -gravityScale);

        for (int i = 0; i < currRopeSize; ++i)
        {
            RopeSegment currSegment = ropeSegments[i];

            // Calculate the current velocity of the segment
            Vector2 velocity = currSegment.posNow - currSegment.posOld;
            currSegment.posOld = currSegment.posNow;

            // Apply gravity to simulate downward movement
            currSegment.posNow += velocity;
            currSegment.posNow += forceGravity * Time.fixedDeltaTime;

            // Update the segment's position with the simulated values
            ropeSegments[i] = currSegment;

            // Apply constraints to maintain the rope's structure
            ApplyConstraint();
        }
    }

    ////////////////////////////////////////////////////////////////////////
    // APPLY CONSTRAINT ====================================================

    // Apply physical contraints to the rope, to keep each end tethered to...
    // ...the partner/player
    private void ApplyConstraint()
    {
        // Tether the first segment to the player's position
        RopeSegment firstSegment = ropeSegments[0];
        firstSegment.posNow = Info.player.transform.position;
        ropeSegments[0] = firstSegment;

        // Tether the last segment to the partner's position
        RopeSegment endSegment = ropeSegments[currRopeSize - 1];
        endSegment.posNow = Info.partner.transform.position;
        ropeSegments[currRopeSize - 1] = endSegment;

        // Apply constraints between adjacent rope segments
        for (int i = 0; i < currRopeSize - 1; ++i)
        {
            RopeSegment currSegment = ropeSegments[i];
            RopeSegment nextSegment = ropeSegments[i + 1];

            // Calculate the current distance between adjacent segments
            float dist = (currSegment.posNow - nextSegment.posNow).magnitude;

            // Calculate the deviation from the desired segment length
            float error = Mathf.Abs(dist - segmentLength);

            // Determine the direction of change
            Vector2 changeDir = Vector2.zero;

            if (dist > segmentLength)
                changeDir = (currSegment.posNow - nextSegment.posNow).normalized;

            else if (dist < segmentLength)
                changeDir = (nextSegment.posNow - currSegment.posNow).normalized;

            // Calculate the amount of position adjustment
            Vector2 changeAmount = changeDir * error;

            // Apply adjustment to maintain segment length
            if (i != 0)
            {
                currSegment.posNow -= changeAmount * 0.5f;
                ropeSegments[i] = currSegment;
                nextSegment.posNow += changeAmount * 0.5f;
                ropeSegments[i + 1] = nextSegment;
            }
            else
            {
                // Adjust the next segment's position at the start of the rope
                nextSegment.posNow += changeAmount;
                ropeSegments[i + 1] = nextSegment;
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////
    // DRAW ROPE ===============================================================
    private void DrawRope()
    {
        // Set the width of the rope segments for rendering
        float width = lineWidth;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;

        // Prepare an array to store the positions of the rope segments
        Vector3[] ropePositions = new Vector3[maxRopeSize];

        // Copy the current positions of rope segments into the array
        for (int i = 0; i < currRopeSize; ++i)
            ropePositions[i] = ropeSegments[i].posNow;

        // Update the LineRenderer's position count and set the positions
        lineRenderer.positionCount = currRopeSize;
        lineRenderer.SetPositions(ropePositions);
    }
}