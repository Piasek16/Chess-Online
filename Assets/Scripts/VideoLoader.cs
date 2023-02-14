using UnityEngine;
using UnityEngine.Video;

public class VideoLoader : MonoBehaviour {
	[SerializeField] private VideoPlayer m_videoPlayer;
	public string VideoFileName;

	void Start() {
		string videoUrl = Application.streamingAssetsPath + "/" + VideoFileName;
		m_videoPlayer.url = videoUrl;
	}
}
