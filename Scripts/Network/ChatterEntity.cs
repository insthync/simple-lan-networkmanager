using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;
using UnityEngine.UI;

public class ChatterEntity : LiteNetLibBehaviour
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

    private void Awake()
    {
        if (chatBubbleRoot != null)
            chatBubbleRoot.SetActive(false);

        if (lastShowEmoticon != null)
            lastShowEmoticon.SetActive(false);
    }

    private void Start()
    {
        if (IsOwnerClient)
            Local = this;
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

    public void CmdSendChat(string message)
    {
        CallNetFunction(_CmdSendChat, FunctionReceivers.Server, message);
    }

    [NetFunction]
    protected void _CmdSendChat(string message)
    {
        RpcShowChat(message);
    }

    public void RpcShowChat(string message)
    {
        CallNetFunction(_RpcShowChat, FunctionReceivers.All, message);
    }

    [NetFunction]
    protected void _RpcShowChat(string message)
    {
        // Set chat text and show chat bubble
        if (chatBubbleText != null)
            chatBubbleText.text = message;

        if (chatBubbleRoot != null)
            chatBubbleRoot.SetActive(true);

        lastShowChatBubbleTime = Time.realtimeSinceStartup;

        // TODO: Add chat message to chat history (maybe in any network manager)
    }

    public void CmdSendEmoticon(int id)
    {
        CallNetFunction(_CmdSendEmoticon, FunctionReceivers.Server, id);
    }

    [NetFunction]
    protected void _CmdSendEmoticon(int id)
    {
        RpcShowEmoticon(id);
    }

    public void RpcShowEmoticon(int id)
    {
        CallNetFunction(_RpcShowEmoticon, FunctionReceivers.All, id);
    }

    [NetFunction]
    protected void _RpcShowEmoticon(int id)
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
