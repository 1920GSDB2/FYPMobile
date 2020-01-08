﻿using UnityEngine;
using System.Collections.Generic;

// This class manages which player behaviour is active or overriding, and call its local functions.
// Contains basic setup and common functions used by all the player behaviours.
public class BasicBehaviour : MonoBehaviour
{
	public Transform playerCamera;                  // Reference to the camera that focus the player.
	public float turnSmoothing = 0.06f;             // Speed of turn when moving to match camera facing.

	float h;                                        // Horizontal Axis.
	float v;                                        // Vertical Axis.
	int currentBehaviour;                           // Reference to the current player behaviour.
	int defaultBehaviour;                           // The default behaviour of the player when any other is not active.
	int behaviourLocked;                            // Reference to temporary locked behaviour that forbids override.
    [HideInInspector]
	public Vector3 lastDirection;                   // Last direction the player was moving.
	Animator anim;                                  // Reference to the Animator component.
	ThirdPersonOrbitCamBasic camScript;             // Reference to the third person camera script.
	bool sprint;                                    // Boolean to determine whether or not the player activated the sprint mode.
	int hFloat;                                     // Animator variable related to Horizontal Axis.
	int vFloat;                                     // Animator variable related to Vertical Axis.
	List<GenericBehaviour> behaviours;              // The list containing all the enabled player behaviours.
	List<GenericBehaviour> overridingBehaviours;    // List of current overriding behaviours.
	Rigidbody rBody;                                // Reference to the player's rigidbody.
	int groundedBool;                               // Animator variable related to whether or not the player is on the ground.
    public LayerMask ignorePlayerLayerMask;         // The layer mask without player
    public int fallHeight { get; private set; }     // Animator variable related to the height of character fall on ground

    public Vector3 colExtents { get; private set; } // Collider extents for ground test.

	// Get current horizontal and vertical axes.
	public float GetH { get { return h; } }
	public float GetV { get { return v; } }

	// Get the player camera script.
	public ThirdPersonOrbitCamBasic GetCamScript { get { return camScript; } }

	// Get the player's rigid body.
	public Rigidbody GetRigidBody { get { return rBody; } }

	// Get the player's animator controller.
	public Animator GetAnim { get { return anim; } }

	// Get current default behaviour.
	public int GetDefaultBehaviour {  get { return defaultBehaviour; } }

	void Awake ()
	{
		// Set up the references.
		behaviours = new List<GenericBehaviour> ();
		overridingBehaviours = new List<GenericBehaviour>();
		anim = GetComponent<Animator> ();
		hFloat = Animator.StringToHash("H");
		vFloat = Animator.StringToHash("V");
		camScript = playerCamera.GetComponent<ThirdPersonOrbitCamBasic> ();
		rBody = GetComponent<Rigidbody> ();

		// Grounded verification variables.
		groundedBool = Animator.StringToHash("Grounded");
        fallHeight = Animator.StringToHash("FallHeight");
        colExtents = GetComponent<CapsuleCollider>().bounds.extents;
	}

	void Update()
	{
		// Store the input axes.
		h = Input.GetAxis("Horizontal");
		v = Input.GetAxis("Vertical");

		// Set the input axes on the Animator Controller.
		anim.SetFloat(hFloat, h, 0.1f, Time.deltaTime);
		anim.SetFloat(vFloat, v, 0.1f, Time.deltaTime);

		// Toggle sprint by input.
		sprint = Input.GetKey(KeyCode.LeftShift);

		// Set the grounded test on the Animator Controller.
		anim.SetBool(groundedBool, IsGrounded());
	}

	// Call the FixedUpdate functions of the active or overriding behaviours.
	void FixedUpdate()
	{
		// Call the active behaviour if no other is overriding.
		bool isAnyBehaviourActive = false;
		if (behaviourLocked > 0 || overridingBehaviours.Count == 0)
		{
			foreach (GenericBehaviour behaviour in behaviours)
			{
				if (behaviour.isActiveAndEnabled && currentBehaviour == behaviour.GetBehaviourCode())
				{
					isAnyBehaviourActive = true;
					behaviour.LocalFixedUpdate();
				}
			}
		}
		// Call the overriding behaviours if any.
		else
		{
			foreach (GenericBehaviour behaviour in overridingBehaviours)
				behaviour.LocalFixedUpdate();
		}

		// Ensure the player will stand on ground if no behaviour is active or overriding.
		if (!isAnyBehaviourActive && overridingBehaviours.Count == 0)
		{
			rBody.useGravity = true;
			Repositioning ();
		}
	}

	// Call the LateUpdate functions of the active or overriding behaviours.
	private void LateUpdate()
	{
		// Call the active behaviour if no other is overriding.
		if (behaviourLocked > 0 || overridingBehaviours.Count == 0)
		{
			foreach (GenericBehaviour behaviour in behaviours)
			{
				if (behaviour.isActiveAndEnabled && currentBehaviour == behaviour.GetBehaviourCode())
				{
					behaviour.LocalLateUpdate();
				}
			}
		}
		// Call the overriding behaviours if any.
		else
		{
			foreach (GenericBehaviour behaviour in overridingBehaviours)
			{
				behaviour.LocalLateUpdate();
			}
		}

	}

	// Put a new behaviour on the behaviours watch list.
	public void SubscribeBehaviour(GenericBehaviour behaviour)
	{
		behaviours.Add (behaviour);
	}

	// Set the default player behaviour.
	public void RegisterDefaultBehaviour(int behaviourCode)
	{
		defaultBehaviour = behaviourCode;
		currentBehaviour = behaviourCode;
	}

	// Attempt to set a custom behaviour as the active one.
	// Always changes from default behaviour to the passed one.
	public void RegisterBehaviour(int behaviourCode)
	{
		if (currentBehaviour == defaultBehaviour)
			currentBehaviour = behaviourCode;
	}

	// Attempt to deactivate a player behaviour and return to the default one.
	public void UnregisterBehaviour(int behaviourCode)
	{
		if (currentBehaviour == behaviourCode)
			currentBehaviour = defaultBehaviour;
	}

	// Attempt to override any active behaviour with the behaviours on queue.
	// Use to change to one or more behaviours that must overlap the active one (ex.: aim behaviour).
	public bool OverrideWithBehaviour(GenericBehaviour behaviour)
	{
		// Behaviour is not on queue.
		if (!overridingBehaviours.Contains(behaviour))
		{
			// No behaviour is currently being overridden.
			if (overridingBehaviours.Count == 0)
			{
				// Call OnOverride function of the active behaviour before overrides it.
				foreach (GenericBehaviour overriddenBehaviour in behaviours)
				{
					if (overriddenBehaviour.isActiveAndEnabled && currentBehaviour == overriddenBehaviour.GetBehaviourCode())
					{
						overriddenBehaviour.OnOverride();
						break;
					}
				}
			}
			// Add overriding behaviour to the queue.
			overridingBehaviours.Add(behaviour);
			return true;
		}
		return false;
	}

	// Attempt to revoke the overriding behaviour and return to the active one.
	// Called when exiting the overriding behaviour (ex.: stopped aiming).
	public bool RevokeOverridingBehaviour(GenericBehaviour behaviour)
	{
		if (overridingBehaviours.Contains(behaviour))
		{
			overridingBehaviours.Remove(behaviour);
			return true;
		}
		return false;
	}

	// Check if any or a specific behaviour is currently overriding the active one.
	public bool IsOverriding(GenericBehaviour behaviour = null)
	{
		if (behaviour == null)
			return overridingBehaviours.Count > 0;
		return overridingBehaviours.Contains(behaviour);
	}

	// Check if the active behaviour is the passed one.
	public bool IsCurrentBehaviour(int behaviourCode)
	{
		return this.currentBehaviour == behaviourCode;
	}

	// Check if any other behaviour is temporary locked.
	public bool GetTempLockStatus(int behaviourCodeIgnoreSelf = 0)
	{
		return (behaviourLocked != 0 && behaviourLocked != behaviourCodeIgnoreSelf);
	}

	// Atempt to lock on a specific behaviour.
	//  No other behaviour can overrhide during the temporary lock.
	// Use for temporary transitions like jumping, entering/exiting aiming mode, etc.
	public void LockTempBehaviour(int behaviourCode)
	{
		if (behaviourLocked == 0)
			behaviourLocked = behaviourCode;
	}

	// Attempt to unlock the current locked behaviour.
	// Use after a temporary transition ends.
	public void UnlockTempBehaviour(int behaviourCode)
	{
		if(behaviourLocked == behaviourCode)
			behaviourLocked = 0;
	}

	// Common functions to any behaviour:

	// Check if player is sprinting.
	public virtual bool IsSprinting()
	{
		return sprint && IsMoving() && CanSprint();
	}

	// Check if player can sprint (all behaviours must allow).
	public bool CanSprint()
	{
		foreach (GenericBehaviour behaviour in behaviours)
		{
			if (!behaviour.AllowSprint ())
				return false;
		}
		foreach(GenericBehaviour behaviour in overridingBehaviours)
		{
			if (!behaviour.AllowSprint())
				return false;
		}
		return true;
	}

	// Check if the player is moving on the horizontal plane.
	public bool IsHorizontalMoving()
	{
		return h != 0;
	}

	// Check if the player is moving.
	public bool IsMoving()
	{
		return (h != 0) || (v != 0);
	}

	// Put the player on a standing up position based on last direction faced.
	public void Repositioning()
	{
		if(lastDirection != Vector3.zero)
		{
			lastDirection.y = 0;
			Quaternion targetRotation = Quaternion.LookRotation (lastDirection);
			Quaternion newRotation = Quaternion.Slerp(rBody.rotation, targetRotation, turnSmoothing);
			rBody.MoveRotation (newRotation);
		}
	}

	// Function to tell whether or not the player is on ground.
	public bool IsGrounded()
	{ 
		Ray ray = new Ray(transform.position + Vector3.up * colExtents.y, Vector3.down);
        bool grounded = Physics.SphereCast(ray, colExtents.x / 2f, colExtents.y + 0.2f, ignorePlayerLayerMask);
        if (!grounded)
        {
            if (rBody.velocity.y < 0)
            {
                // Detect the vertical distance between player and ground
                RaycastHit hit;
                Physics.SphereCast(ray, colExtents.x / 2f, out hit, transform.position.y + colExtents.y + 0.2f);
                float groundHeight = transform.position.y - hit.transform.position.y;
                if (anim.GetFloat(fallHeight) < groundHeight)
                    anim.SetFloat(fallHeight, groundHeight);
            }
        }
        else
            anim.SetFloat(fallHeight, 0);
        return grounded;
	}
}