// Unity 6 Compatible - MessageNotifier.cs
// Updated: 2026-01-31

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Patterns.Observer;

public class MessageNotifier : MonoBehaviour {

    public Message message;

	public void Notify()
    {
        this.Notify(message);
    }
}
