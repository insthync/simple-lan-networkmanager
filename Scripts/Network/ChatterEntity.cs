using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ChatterEntity : NetworkBehaviour
{
    public static ChatterEntity Local { get; private set; }
    [Header("Chat Bubble")]
    public float chatBubbleVisibleDuration = 2f;
    public GameObject chatBubbleRoot;
    public Text chatBubbleText;
    [Header("Emoticons")]
    public float emoticonVisibleDuration = 2f;
    public GameObject[] emoticons;

    private float lastShowChatBubbleTime;
    private float lastShowEmoticonTime;
    private GameObject lastShowEmoticon;

    public override void OnStartLocalPlayer()
    {
        Local = this;
    }

    private void Awake()
    {
        if (chatBubbleRoot != null)
            chatBubbleRoot.SetActive(false);

        if (lastShowEmoticon != null)
            lastShowEmoticon.SetActive(false);
    }

    private void Update()
    {
        // Hide chat bubble
        if (Time.realtimeSinceStartup - lastShowChatBubbleTime >= chatBubbleVisibleDuration)
        {
            if (chatBubbleRoot != null)
                chatBubbleRoot.SetActive(false);
        }
        // Hide emoticon
        if (Time.realtimeSinceStartup - lastShowEmoticonTime >= emoticonVisibleDuration)
        {
            if (lastShowEmoticon != null)
                lastShowEmoticon.SetActive(false);
        }
    }

    [Command]
    public void CmdSendChat(string message)
    {
        RpcShowChat(message);
    }

    [ClientRpc]
    public void RpcShowChat(string message)
    {
        // Set chat text and show chat bubble
        if (chatBubbleText != null)
            chatBubbleText.text = message;

        if (chatBubbleRoot != null)
            chatBubbleRoot.SetActive(true);

        lastShowChatBubbleTime = Time.realtimeSinceStartup;

        // TODO: Add chat message to chat history (maybe in any network manager)
    }

    [Command]
    public void CmdSendEmoticon(int id)
    {
        RpcShowEmoticon(id);
    }

    [ClientRpc]
    public void RpcShowEmoticon(int id)
    {
        if (id < 0 || id >= emoticons.Length)
            return;

        // Show emoticon by index
        foreach (var emoticon in emoticons)
        {
            if (emoticon != null)
                emoticon.SetActive(false);
        }

        lastShowEmoticon = emoticons[id];
        if (lastShowEmoticon != null)
            lastShowEmoticon.SetActive(true);

        lastShowEmoticonTime = Time.realtimeSinceStartup;
    }
}
