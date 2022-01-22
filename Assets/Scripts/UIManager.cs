using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject ClothPrefab;

    private float mass;
    private ForceStatus method;

    public void SetMass(string s)
    {
        mass = Convert.ToSingle(s);
    }

    public void SetMethod(int i)
    {
        method = (ForceStatus)i;
    }

    public void AddCloth()
    {
        Instantiate(ClothPrefab);
    }

    public void EditCloth()
    {
        ClothSystem cloth = ControllPntManager.Instance.NowClick.GetComponent<ClothSystem>();
        cloth.Mass = mass;
        cloth.ForceStatus = method;
    }
}
