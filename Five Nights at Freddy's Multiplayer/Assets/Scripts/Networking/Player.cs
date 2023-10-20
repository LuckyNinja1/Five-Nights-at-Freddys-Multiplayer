using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private string username;
    //[SerializeField] private Character character;

    public void SetUsername(string username_)
    {
        username = username_;
    }
    public string GetUsername() { return username; }
    //public Character GetCharacter() { return character; }
}
