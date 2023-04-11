using UnityEngine;
using UnityEngine.Video;

public class VideoLoader : MonoBehaviour {
	[SerializeField] private VideoPlayer videoPlayer;
	public string VideoFileName;

	void Start() {
		string videoUrl = Application.streamingAssetsPath + "/" + VideoFileName;
		videoPlayer.url = videoUrl;
	}
}
