// The code here is basically reference from: http://dirigiballers.blogspot.fr/2013/03/unity-c-audiomanager-tutorial-part-1.html
// You can go there for more information

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This AudioManager class is used to manage the audio and play the corresponding audios when needed
public class AnimationAudioManager : Singleton<AnimationAudioManager> {

	// List of clip audio to play when necessary 
	private List<ClipInfo> m_activeAudio;

	// set the indices of the animation audio
	private int _runAudioIdx, _jumpAudioIdx, _doubleJumpAudioIdx, _getStoneAudioIdx;

	//sfx for animation of the player
	// ATTENTION: we MUST drag and drop the list of sound effect in Unity Inspector
	// adding sfx files dynamically in code only visible in PRO
	public AudioClip[] animationAudio;


	// This ClipInfo class is only accessed by the Audio class (nesting class)
	class ClipInfo
	{
		//ClipInfo used to maintain default audio source info
		public AudioSource source { get; set; }
		public float defaultVolume { get; set; }
	}

	void Awake() {
		//Debug.Log("Animation AudioManager Initializing");
		try {
			//transform.parent = GameObject.FindGameObjectWithTag("Player").transform;
			//transform.localPosition = new Vector3(0, 0, 0);
			m_activeAudio = new List<ClipInfo>();
			_runAudioIdx = 0;
			_jumpAudioIdx = 1;
			_doubleJumpAudioIdx = 2;
			_getStoneAudioIdx = 3;
		} catch {
			Debug.LogError("Unable to find main camera to put audiomanager");
		}
	}

	// These functions play the corresponding sfx depending on the context
	// with the given position and volume
	public void PlayRunSfx(Vector3 soundOrigin, float volume)
	{
		AudioSource.PlayClipAtPoint(animationAudio[_runAudioIdx], soundOrigin, volume);
	}

	public void PlayJumpSfx(Vector3 soundOrigin, float volume)
	{
		AudioSource.PlayClipAtPoint(animationAudio[_jumpAudioIdx], soundOrigin, volume);
	}

	public void PlayDoubleJumpSfx(Vector3 soundOrigin, float volume)
	{
		AudioSource.PlayClipAtPoint(animationAudio[_doubleJumpAudioIdx], soundOrigin, volume);
	}

	public void PlayGetStoneSfx(Vector3 soundOrigin, float volume)
	{
		AudioSource.PlayClipAtPoint(animationAudio[_getStoneAudioIdx], soundOrigin, volume);
	}

	// This function will play the audio clip
	// Usage: AudioManager.Instance.Play(parameters)
	public AudioSource Play(AudioClip clip, Vector3 soundOrigin, float volume) {
		//Create an empty game object
		GameObject soundLoc = new GameObject("Audio: " + clip.name);
		soundLoc.transform.position = soundOrigin;
		
		//Create the source
		AudioSource source = soundLoc.AddComponent<AudioSource>();
		setSource(ref source, clip, volume);
		source.Play();
		Destroy(soundLoc, clip.length);
		
		//Set the source as active
		m_activeAudio.Add(new ClipInfo{source = source, defaultVolume = volume});
		return source;
	}

	// This function sets the properties of the new audio source. 
	private void setSource(ref AudioSource source, AudioClip clip, float volume) {
		source.rolloffMode = AudioRolloffMode.Logarithmic;
		source.dopplerLevel = 0.2f;
		source.minDistance = 150;
		source.maxDistance = 1500;
		source.clip = clip;
		source.volume = volume;
	}
}

