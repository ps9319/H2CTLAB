using System.Collections;
using System.Collections.Generic;
using RenderHeads.Media.AVProVideo;
using UnityEngine;

public class MediaPlayerAddon : MonoBehaviour
{
    private MediaPlayer mediaPlayer;
    [SerializeField] private float playDelay = 0f; // Inspector에서 설정 가능

    void Awake()
    {
        mediaPlayer = GetComponent<MediaPlayer>();
    }
    
    void Start()
    {
        StartCoroutine(PlayAfterDelay());
    }

    private IEnumerator PlayAfterDelay()
    {
        yield return new WaitForSeconds(playDelay);
        mediaPlayer.Control.Play();
    }
}