﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour {
	#region properties
	private Animator _anim;
	//these function is updated by function OnStateChange
	private AnimatorStateInfo _currentBaseState;
	private AnimatorStateInfo _previousBaseState;
	private bool _IsOnChangeState = false;

	//rotate
	public float TurnSmoothly = 1500.0f;
	
	//move
	public float VelocityFactor = 3.0f; // this factor let's velocity * orientation (in range [-1; 1]) increase faster to maximum speed
	private float _velocity = 0.0f;
	public float VelocityMaximum = 8.0f;//5.3f;

	//jump
	public float JumpForce = 17.0f;
	public float JumpForceReduce = 0.7f;// reduce force for every physic frame
	public float DoubleJumpForce = 10.0f;
	public float DoubleJumpForceReduce = 0.7f;// reduce force for every physic frame
	
	private int _jumpCount = 0;
	public int JumpCountMaximum = 2;
	private float _jumpMove = 0.0f;
	private bool _jumpButtonLock = false;//only unlock when release then re-press/touch jump button
	private bool _isKeyboardInput = true;//there are 2 types of input: by keys or by touch-button
	public float jumpFallForcePerFrame = 1.5f;
	public float FallVelocityMaximum = 10.0f;

	//pre-define only for this particular scene
	private Vector3 Vector3Forward { get { return new Vector3(1.0f, 0, 0); } }
    	
	//fake gravity
	//current gravity is not strong enough for this game (when compare with the speed of jumping)
	//I am not sure what is the effect of changing the global gravity.
	//so, I use this addition force in order to make character fall faster, only in locomotion state
	public float FakeGravity = 30.0f;
	
	//mid-air ray-cast check
	public float MidAirCheck = 1.2f;
    
	//control events for current animator
	AnimatorEvents _animatorEvents;

	//animator parameters
	private float _animParamSpeedFloat;
	private bool _animParamJumpBool;
	private bool _animParamFallToLandBool;
	private float _animParamYVelocityFloat;
	private bool _animParamMidAirBool;
	
	private GUIManager guiManager;
	private GameController gameController;
	
	private float lastSynchronizationTime = 0f;
	private float syncDelay = 0f;
	private float syncTime = 0f;
	private Vector3 syncStartPosition = Vector3.zero;
	private Vector3 syncEndPosition = Vector3.zero;
	private Vector3 _destination;
	private bool IsJump;
	//private float moveDirection = 0;
	
	GameObject testMultiplayer = null;

	//sfx for animation of the player
	public AudioClip[] animationAudio;

	// Particles
	public ParticleSystem jump;
	public ParticleSystem pickupItems;

	#endregion
	
	#region gui and network
	void Awake() {
		_anim = GetComponent<Animator>();
		
		guiManager = GameObject.FindGameObjectWithTag(Tags.gui).GetComponent<GUIManager>();
		//guiManager.SetMaxHP(MaxHP);
	
		//control events for current animator
		_animatorEvents = GetComponent<AnimatorEvents>();
		gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
		testMultiplayer = GameObject.Find("Multiplayer Manager");
	}
	
	void FixedUpdate() {
		if (gameController.gameEnd != 0)
		{
			UpdateAnimatorParamametersFrom(_anim);
			this.updateMovement(0, false); //character is idle
			UpdateAnimatorParamametersTo(_anim, _animParamSpeedFloat, _animParamJumpBool, _animParamFallToLandBool,
			                             _animParamYVelocityFloat, _animParamMidAirBool);
			return;
		}

		if (testMultiplayer == null) //Test movement only, single player
        {
            if (gameController.isPause)
                return;

			_animParamYVelocityFloat = rigidbody.velocity.y;
			_animParamMidAirBool = checkMidAir();

			//get all inputs
			//orientation, works with float range [-1.0f, 1.0f]
			float h = Input.GetAxis("Horizontal");
			float hInt = Mathf.Clamp(h + guiManager.GetInputGUI_h(), -1.0f, 1.0f);
			//moveDirection = hInt;
			
			//only set IsJump = true when that button is release and re-press again
			if ((Input.GetButtonUp("Jump") && _isKeyboardInput) //if input from keyboard
			    || (guiManager.GetInputGUI_v() == 0.0f && !_isKeyboardInput)) //if input from touch-button 
			{
				_jumpButtonLock = false;
			}
			//jump
			IsJump = false;
			if ((Input.GetButtonDown("Jump") || guiManager.GetInputGUI_v() != 0.0f)
			    && !_jumpButtonLock)
			{
				IsJump = true;
				_jumpButtonLock = true;
				_isKeyboardInput = Input.GetButtonDown("Jump") ? true : false;
			}

			if (IsJump) {
				//print("IsJump " + IsJump);
			}

			UpdateAnimatorParamametersFrom(_anim);
            this.updateMovement(hInt, IsJump);
			UpdateAnimatorParamametersTo(_anim, _animParamSpeedFloat, _animParamJumpBool, _animParamFallToLandBool,
			                             _animParamYVelocityFloat, _animParamMidAirBool);
		}
		else
		{
			//States in server is the correct one for all network player (regardless networkView), all clients must follow
	
			//Input only for network player of owner
			if (networkView.isMine)
			{
                if (gameController.isPause)
                    return;

				_animParamYVelocityFloat = rigidbody.velocity.y;
				_animParamMidAirBool = checkMidAir();

				//get all inputs
				//orientation, works with float range [-1.0f, 1.0f]
				float h = Input.GetAxis("Horizontal");
				float hInt = Mathf.Clamp(h + guiManager.GetInputGUI_h(), -1.0f, 1.0f);
				//moveDirection = hInt;
				
				//only set IsJump = true when that button is release and re-press again
				if ((Input.GetButtonUp("Jump") && _isKeyboardInput) //if input from keyboard
				    || (guiManager.GetInputGUI_v() == 0.0f && !_isKeyboardInput)) //if input from touch-button 
				{
					_jumpButtonLock = false;
				}
				//jump
				IsJump = false;
				if ((Input.GetButtonDown("Jump") || guiManager.GetInputGUI_v() != 0.0f)
				    && !_jumpButtonLock)
				{
					IsJump = true;
					_jumpButtonLock = true;
					_isKeyboardInput = Input.GetButtonDown("Jump") ? true : false;
				}
				
				UpdateAnimatorParamametersFrom(_anim);
                this.updateMovement(hInt, IsJump);
				UpdateAnimatorParamametersTo(_anim, _animParamSpeedFloat, _animParamJumpBool, _animParamFallToLandBool,
				                             _animParamYVelocityFloat, _animParamMidAirBool);							
			}
			 else
            {
                    SyncedMovement();
            } 
		}
	}
	
	void OnGUI()
	{
		//guiManager.UpdateHP(HP,-1);// negative is left HP, positive is right HP, depend on which side player is.
		guiManager.UpdateTouchInput();
	}

    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        float syncVelocity = 0;
        Vector3 syncRigidVelocity = Vector3.zero;
        Vector3 syncPosition = Vector3.zero;
        Quaternion syncRotation = Quaternion.identity;

        bool syncJumpBool = false;
        //bool syncIsDoubleJump = false;
        bool syncFallToLandBool = false;
        float syncSpeedFloat = 0;

		float syncYVelocityFloat = 0.0f;
		bool syncIsMidAirBool = false;

        if (stream.isWriting)
        {
            syncVelocity = this._velocity;
            syncRigidVelocity = rigidbody.velocity;
            syncPosition = transform.position;
            syncRotation = transform.rotation;
            syncJumpBool = _animParamJumpBool;
            //syncIsDoubleJump = ;
            syncFallToLandBool = _animParamFallToLandBool;
            syncSpeedFloat = _animParamSpeedFloat;
			syncYVelocityFloat = _animParamYVelocityFloat;
			syncIsMidAirBool = _animParamMidAirBool;

			stream.Serialize(ref syncFallToLandBool);

            stream.Serialize(ref syncVelocity);
            stream.Serialize(ref syncRigidVelocity);
            stream.Serialize(ref syncPosition);
            stream.Serialize(ref syncRotation);
            stream.Serialize(ref syncJumpBool);
            //stream.Serialize(ref syncIsDoubleJump);
            
            stream.Serialize(ref syncSpeedFloat);

			stream.Serialize(ref syncIsMidAirBool);
			stream.Serialize(ref syncYVelocityFloat);

		}
		if (stream.isReading)
        {
			stream.Serialize(ref syncFallToLandBool);

			stream.Serialize(ref syncVelocity);
            stream.Serialize(ref syncRigidVelocity);
            stream.Serialize(ref syncPosition);
            stream.Serialize(ref syncRotation);
            stream.Serialize(ref syncJumpBool);
            //stream.Serialize(ref syncIsDoubleJump);
            stream.Serialize(ref syncSpeedFloat);

			stream.Serialize(ref syncIsMidAirBool);
			stream.Serialize(ref syncYVelocityFloat);


            syncTime = 0;
            syncDelay = Time.time - lastSynchronizationTime;
            lastSynchronizationTime = Time.time;

            this._velocity = syncVelocity;
            transform.rotation = syncRotation;
            _animParamJumpBool = syncJumpBool;
            //isDoubleJump = syncIsDoubleJump;
            _animParamFallToLandBool = syncFallToLandBool;
            _animParamSpeedFloat = syncSpeedFloat;
			_animParamYVelocityFloat = syncYVelocityFloat;
			_animParamMidAirBool = syncIsMidAirBool;

            syncEndPosition = syncPosition + syncRigidVelocity * syncDelay;
            syncStartPosition = transform.position;

			UpdateAnimatorParamametersTo(_anim, _animParamSpeedFloat, _animParamJumpBool, _animParamFallToLandBool,
			                             _animParamYVelocityFloat, _animParamMidAirBool);
        }
    }

    void SyncedMovement()
    {
        syncTime += Time.deltaTime;
        transform.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
    }
    
	#endregion

	#region animator events
	//------------------------------------------------------------------------------
	//control events of current animator
	void OnEnable(){
		//EventtriggersfromAnimatorEvents
		_animatorEvents.OnStateChanged += OnStateChanged;
		_animatorEvents.OnTransition += OnTransition;
	}
	
	void OnDisable(){
		_animatorEvents.OnStateChanged -= OnStateChanged;
		_animatorEvents.OnTransition -= OnTransition;
	}
	//------------------------------------------------------------------------------
	/// <summary>
	/// Implemented by an animation event plugin. 
	/// If there is any change in state, this function will be called
	/// </summary>
	/// <param name="layer">Layer.</param>
	/// <param name="previous">Previous state machine</param>
	/// <param name="current">Current state machine</param>
	void OnStateChanged(int layer, AnimatorStateInfo previous,AnimatorStateInfo current){
		_currentBaseState = current;
		_previousBaseState = previous;
		_IsOnChangeState = true;
		return;
		//This displays the State Info of previous and currentstates.
		//Debug.Log("State changed from" + previous + "to" + current);
		
		//AnimatorEvents returns a much friendly way than hash names
		//Debug.Log("State changed to" + animatorEvents.layers[layer].GetStateName(current.nameHash));
	}
	
	void OnTransition(int layer, AnimatorTransitionInfo transitionInfo){
		//		Debug.Log("Transition from"+ animatorEvents.layers[layer].GetTransitionName(transitionInfo.nameHash));
		//print(animatorEvents.layers[layer].GetTransitionName(transitionInfo.nameHash) + "at framecount: " + counter);
		//		if (currentBaseState.nameHash == PlayerHashIDs.jumpState) {
		//			//print(animatorEvents.layers[layer].GetTransitionName(transitionInfo.nameHash));
		//		}
		//problem: different frame number of [the time of pressed button jump] >> [the end of jump state] leads to difference jump height.
		//solve: combine idle state into locomotion state.
		//because in the transition [locomotion] >> [idle], if we jump, jumpForce will be applied at that time 
		//but it takes extra frames to make change [in-middle-of-transition locomotion >> idle] >> [jump state]
	}
	#endregion
	private AnimatorTransitionInfo _lastTransition;
	private int _frameCounter = 0;
	private int _frameTransitionCounter = 0;
	public void updateMovement(float horizontal, bool IsJump) {
		int layer = 0;
//		for ( int layer = 0; layer < layers.Length; layer++) {
//		if (AnimatorEvents.layers[layer].isListening) {
			// State Change Verification
		_currentBaseState = _anim.GetCurrentAnimatorStateInfo(layer);
		
		if (_previousBaseState.nameHash != _currentBaseState.nameHash) {
			//print("nframe: " + _frameCounter + " nframTransition: " + _frameTransitionCounter);
			//print("... >> [" 
			//      +  _animatorEvents.layers[0].GetStateName(_currentBaseState.nameHash)
			//      + "]");
			_frameCounter = 0;// reset counter
			_frameTransitionCounter = 0; //reset transition cunter too, because this frame just after transition
			OnStateChanged (layer, _previousBaseState, _currentBaseState);
			_previousBaseState = _currentBaseState;
		}
		else {
			_frameCounter++;
		}
		if (_anim.IsInTransition(layer)) {
			_frameTransitionCounter++;
			if (_lastTransition.nameHash != _anim.GetAnimatorTransitionInfo(layer).nameHash) {
				//print("transition: " +_animatorEvents.layers[0].GetTransitionName(_anim.GetAnimatorTransitionInfo(0).nameHash));
			}
			this.OnTransition(layer, _anim.GetAnimatorTransitionInfo(layer));
			_lastTransition = _anim.GetAnimatorTransitionInfo(layer);
		}
//		}
//		}

		//get all inputs
		//get state init, after is override by OnStateChange
		//if (currentBaseState.nameHash != anim.GetCurrentAnimatorStateInfo(0).nameHash) print("khac: ");
		//if (currentBaseState == null)
		//currentBaseState = anim.GetCurrentAnimatorStateInfo(0);	// set our currentState variable to the current state of the Base Layer (0) of animation
		
		MovementManagement(horizontal);
		jumpManagement(horizontal, IsJump);
		//		if (this.rigidbody.velocity.y > 0.1f || this.rigidbody.velocity.z > 0.1f) {
		//			print ("velocity: " + this.rigidbody.velocity + ", hInt " + horizontal);
		//		}

	}
	
	#region movement management
	void MovementManagement(float orientation) {
		Rotation (orientation);
		if (orientation != 0.0f) {
			this._velocity = Mathf.Clamp(VelocityFactor * VelocityMaximum * orientation, -VelocityMaximum, VelocityMaximum);
		}
		else {
			//not set velocity to zero immediately, but slow it down a bit
			//It's solve the problem: when we change the orientation, 
			//	there is 1 frame that the orientation becomes 0, 
			//	character's state change to idle before back to locomotion
			if (Mathf.Abs(_velocity) < 0.05) {
				_velocity = 0.0f;
			}
			else {
				_velocity /= 2.0f; // around 5 physic frames
			}
		}
		
		//if set value into velocity, the value will reset each frame
		//this.rigidbody.velocity +=  this.Vector3Forward * this.velocity;
		this.rigidbody.AddForce(this.Vector3Forward * this._velocity, ForceMode.VelocityChange);
		//anim.SetFloat(PlayerHashIDs.speedFloat, Mathf.Abs(velocity));
		_animParamSpeedFloat = Mathf.Abs(_velocity);
		//changing the whole rigidbody by chaging the velocity, depend on mass
		//this.rigidbody.AddForce(Vector3.forward * orientation * (velocity * this.rigidbody.mass), ForceMode.Impulse);
		//this.rigidbody.AddForce(this.Vector3Forward * orientation * (velocity * this.rigidbody.mass) * 50.0f, ForceMode.Force);
		//don't know why we need alot of force to move character
		
		//with the velocity, its almost the same, I think
		//this.rigidbody.velocity = Vector3.forward * velocity * horizontal;
		//addForce applies into the value of rigid.velocity. Checked
		//print ("velocity: " + this.rigidbody.velocity);
	}
	
	/// <summary>
	/// Rotate character when orientation from negative to positive and vice versa
	/// </summary>
	/// <param name="orientation">Orientation.</param>
	void Rotation(float orientation) {
		if (orientation == 0.0f) return;
		Vector3 targetDirection = new Vector3(orientation, 0.0f, 0.0f);
		Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
		
		Quaternion newRotation = Quaternion.Lerp(this.rigidbody.rotation, targetRotation, TurnSmoothly * Time.deltaTime);
		this.rigidbody.MoveRotation(newRotation);
	}
	#endregion 
	
	#region manage jump state variable
	void jumpStateEnter() {
		Vector3 force = Vector3.zero;

		if(_jumpCount == 0) {//1st jump
			_animParamJumpBool = true;
			//print("active jump!");

			force += Vector3.up * JumpForce;
            if(gameController.isSoundEnable)
			    AnimationAudioManager.Instance.PlayJumpSfx(transform.position, 1.0f);

			//play particle jump
			PlayJump(this.transform.transform.position,Quaternion.AngleAxis(90,new Vector3(1,0,0)));
		}
		//enable double jump animation
		else if (_jumpCount == 1) {//2nd jump
				_animParamJumpBool = true;
				//print ("active double jump!");
				
				//reset old down-force
				Vector3 forceRemover = Vector3.zero;
				forceRemover.y = -this.rigidbody.velocity.y;
				this.rigidbody.AddRelativeForce(forceRemover, ForceMode.VelocityChange);

                force += Vector3.up * DoubleJumpForce;
                if (gameController.isSoundEnable)			
				    AnimationAudioManager.Instance.PlayDoubleJumpSfx(transform.position, 1.0f);

			//play particle jump
			PlayJump(this.transform.transform.position,Quaternion.AngleAxis(90,new Vector3(1,0,0)));
		}

		_jumpMove = _velocity;
		force += Vector3Forward * _jumpMove;
		this.rigidbody.AddRelativeForce(force, ForceMode.VelocityChange);

		_jumpCount++;
		//jumpForce = force for destroying gravity + force depend on vlocity and mass
		//idle, jump at force 5.0f, walk/run jump at force up to 5 + 5.3/2
		//jumpForce = 9.8f + 50.0f + this.rigidbody.mass * velocity / 2.0f;
		//this.rigidbody.velocity = new Vector3(this.rigidbody.velocity.x, jumpHeight, this.rigidbody.velocity.z);
	}
	
	void jumpStateReset() {
		_animParamJumpBool = false;
		_animParamFallToLandBool = false;
		_jumpCount = 0;
	}
	#endregion
	
	#region manage jump process by check related state
	
	//three basic steps for jumping process
	//step 1: jump with a vector-up-force and vector-forward-force, controlled by orientation, in 1 second
	//step 2: fall down with a raycast, change to landing state (FallToLand = true) when almost ground
	//step 3: do the animation, reset variables (jumpCount = 0, FallToLand = false)
	void jumpManagement(float orientation, bool IsJump) {
		if (_currentBaseState.nameHash == PlayerHashIDs.locomotionState) jumpManagementCheckLocomotionState();
		else if (_currentBaseState.nameHash == PlayerHashIDs.jumpState) jumpManagementCheckJumpState();
		else if (_currentBaseState.nameHash == PlayerHashIDs.doubleJumpState) jumpManagementCheckDoubleJumpState();
		else if (_currentBaseState.nameHash == PlayerHashIDs.fallState) jumpManagementCheckFallState();
		//else if (_currentBaseState.nameHash == PlayerHashIDs.landState) jumpManagementCheckLandState();
		
		_IsOnChangeState = false;
	}
	
	void jumpManagementCheckLocomotionState() {
		if (_IsOnChangeState) { //change from other state to this state
			//this.jumpStateReset();//was reseted in transition [fall] >> [locomotion]
			//return;
			if (_animParamJumpBool) { //pressed jump button in transition [fall] >> [locomotion]
				//let's it go
			}
			else {
				this.jumpStateReset();
			}
		}
		
		if (IsJump && _jumpCount < JumpCountMaximum) {//transition to [jump] immediately
			this.jumpStateEnter();
			return;
		}

		if (!_anim.IsInTransition(0)) { //not apply in transition, for example: [locomotion] >> [jump]
			// Raycast down from the center of the character.. FOR FAKE GRAVITY ONLY IN LOCOMOTION STATE
			bool isMidAir = this.checkMidAir();
			if (isMidAir) {
				this.rigidbody.AddRelativeForce(Vector3.down * FakeGravity, ForceMode.Force);
			}
		}
	}
	
	void jumpManagementCheckJumpState() {
		if (_IsOnChangeState) { //change from other state to this state
			//reset JumpBool in animator, in order to re-jump in jump or fall state
			if (_jumpCount == 1) { //1st jump
				//reset, in order to transition to [fall]
				_animParamJumpBool = false;
			}
			else if (_jumpCount == 2) { //2nd jump
				//let's it go, wait to transition to [double jump] in next frame
				return;
			}
		}

		//check double jump
		if (IsJump && _jumpCount < JumpCountMaximum) {
			this.jumpStateEnter();
			//print ("receive double jump at " + _animatorEvents.layers[0].GetStateName(_currentBaseState.nameHash));
		}
		
		this.rigidbody.AddRelativeForce(Vector3.down * JumpForceReduce, ForceMode.VelocityChange);
	}
	
	void jumpManagementCheckDoubleJumpState() {
		if (_IsOnChangeState) { //change from other state to this state
			//reset DoubleJump, so when DoubleJump > FallState, FallState doesn't go back to DoubleJumpState
			_animParamJumpBool = false;
		}

		//redure doubleJumpForce per frame, in order to make character's trajectory look like parabol.
		this.rigidbody.AddRelativeForce(Vector3.down * DoubleJumpForceReduce, ForceMode.VelocityChange);
	}


	void jumpManagementCheckFallState() {
		if (_IsOnChangeState) { //change from other state to this state
			//if (!_animParamDoubleJumpBool)//(!ActiveJumpCommand())
			if (!_animParamJumpBool) { //if not double jump, reset force from jumping!
				this.rigidbody.AddRelativeForce(Vector3.down * Mathf.Abs(this.rigidbody.velocity.y), ForceMode.VelocityChange);
				
				Vector3 force = Vector3.zero;
				force += Vector3Forward * _jumpMove;
				
				this.rigidbody.AddRelativeForce(force, ForceMode.VelocityChange);
			}
			return;
		}

		//Debug.Log("fall veclocity: " + this.rigidbody.velocity);
		if (-this.rigidbody.velocity.y < FallVelocityMaximum) {
			this.rigidbody.AddRelativeForce(Vector3.down * jumpFallForcePerFrame, ForceMode.VelocityChange);
		}
		else {
			//in case fall velocity already get its limit, try to remove the effect of gravity to keep
			//the fall velocity around fixed value FallVelocityMaximum
			this.rigidbody.AddRelativeForce(-Physics.gravity / 50.0f, ForceMode.VelocityChange);
		}

		bool isMidAir = this.checkMidAir();
        if (!isMidAir && !_anim.IsInTransition(0))
        {// first frame of transition [fall] >> [locomotion]
            this.jumpStateReset();
			_animParamFallToLandBool = true; //reset everything except this one
			//prepare for transition [fall] >> [locomotion] in next frame
			//in this transition, if IsJump, as soon as its state is locomotion, state will change auto to jump
		}

		if (!_anim.IsInTransition(0)) {
            if (IsJump && _jumpCount < JumpCountMaximum)
            {
                this.jumpStateEnter();
                //print ("receive double jump at " + _animatorEvents.layers[0].GetStateName(_currentBaseState.nameHash));
            }
		}
		else {// almost landing, not allow double jump
			if (IsJump && _jumpCount == 0) {
				this.jumpStateEnter();
				//print ("receive jump at " + _animatorEvents.layers[0].GetStateName(_currentBaseState.nameHash));
			}
		}
	}

    bool checkMidAir()
    {
        // Raycast down from the center of the character.. 
        Ray ray = new Ray(this.transform.position + Vector3.up, Vector3.down);
        RaycastHit hitInfo = new RaycastHit() ;

        if (Physics.Raycast(ray, out hitInfo))
        {
            //Debug.Log(hitInfo.distance);
            //print(hitInfo.distance);
            if (hitInfo.distance < MidAirCheck)
            {//this value may change depend on character's center
                //Debug.Log("NO");
                return false;
            }
        }
        //Debug.Log("YESSS");
        return true;
    }

    
	
//	void jumpManagementCheckLandState() {
//		if (_IsOnChangeState) { //change from other state to this state
//			ActiveJumpCommand();
//			return;
//		}
//		
//		//check double jump
//		receiveDoubleJumpCommand();
//	}
	#endregion

	#region update animator of Mecanim system
	/// <summary>
	/// update from animator to local variables at the beginning of FixedUpdate
	/// </summary>
	/// <param name="animParam">Animation parameter.</param>
	void UpdateAnimatorParamametersFrom(Animator animParam) {
		_animParamSpeedFloat = animParam.GetFloat(PlayerHashIDs.speedFloat);
		_animParamJumpBool = animParam.GetBool(PlayerHashIDs.JumpBool);
		_animParamFallToLandBool = animParam.GetBool(PlayerHashIDs.FallToLandBool);
		//_animParamYVelocityFloat = animParam.GetFloat(PlayerHashIDs.YVelocityFloat);
		//_animParamMidAirBool = animParam.GetBool(PlayerHashIDs.MidAirBool);
	}

	/// <summary>
	/// After receiving input data, current state, we decide the new value for local variables
	/// Then, update from local to Animator Parameters at the end of FixedUpdate
	/// </summary>
	/// <param name="animParam">Animation parameter.</param>
	/// <param name="speedFloat">Speed float.</param>
	/// <param name="jumpBool">If set to <c>true</c> jump bool.</param>
	/// <param name="fallToLandBool">If set to <c>true</c> fall to land bool.</param>
	/// <param name="doubleJumpBool">If set to <c>true</c> double jump bool.</param>
	void UpdateAnimatorParamametersTo(Animator animParam, 
	                                float speedFloat, bool jumpBool, bool fallToLandBool,
	                                  float YVelocityFloat, bool isMidAirBool) {
		animParam.SetFloat(PlayerHashIDs.speedFloat, speedFloat);
		animParam.SetBool(PlayerHashIDs.JumpBool, jumpBool);
		animParam.SetBool(PlayerHashIDs.FallToLandBool, fallToLandBool);
		//animParam.SetBool(PlayerHashIDs.DoubleJumpBool, doubleJumpBool);

		animParam.SetFloat(PlayerHashIDs.YVelocityFloat, YVelocityFloat);
		animParam.SetBool(PlayerHashIDs.MidAirBool, isMidAirBool);
	}

    /// <summary>
    /// Use this when respawn player
    /// </summary>
    public void ResetAllStates()
    {
        rigidbody.velocity = Vector3.zero;
        UpdateAnimatorParamametersTo(_anim, 0, false, false, 0.0f, false);
    }
	#endregion

	#region Particle system
	void PlayJump(Vector3 pos, Quaternion rot)
	{
		instantiate(jump,pos,rot);
	}
	
	private ParticleSystem instantiate(ParticleSystem prefab, Vector3 position, Quaternion rot)
	{
		ParticleSystem newParticleSystem = Instantiate(prefab, position, rot) as ParticleSystem;
		// Make sure it will be destroyed
		Destroy(newParticleSystem.gameObject, newParticleSystem.duration);
		return newParticleSystem;
	}
	#endregion
}