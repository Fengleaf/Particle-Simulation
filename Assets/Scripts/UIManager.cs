using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject ClothPrefab;

    public InputField MassInputField;
    public Dropdown MethodDropDown;
    public InputField SideLengthInputField;
    public InputField SpacingInputField;
    public InputField XInputField;
    public InputField YInputField;
    public InputField ZInputField;

    public Button EditButton;
    public Button LockButton;
    public Button AddButton;
    public Button DeleteButton;

    public Toggle SpringVisibilityToggle;
    public Toggle ParticleVisibilityToggle;
    public Toggle TextureVisibilityToggle;

    public Button PlayButton;

    private float mass;
    private ForceStatus method;

    private int sideLength;
    private float space;
    private Vector3 initialPosition;

    private bool isPlaying;

    private void Start()
    {
        MassInputField.onEndEdit.AddListener(SetMass);
        MethodDropDown.onValueChanged.AddListener(SetMethod);
        SideLengthInputField.onEndEdit.AddListener(SetSideLength);
        SpacingInputField.onEndEdit.AddListener(SetSpacing);
        XInputField.onEndEdit.AddListener(SetInitialX);
        YInputField.onEndEdit.AddListener(SetInitialY);
        ZInputField.onEndEdit.AddListener(SetInitialZ);
        EditButton.onClick.AddListener(EditCloth);
        LockButton.onClick.AddListener(LockParticle);
        AddButton.onClick.AddListener(AddCloth);
        DeleteButton.onClick.AddListener(DeleteCloth);
        SpringVisibilityToggle.onValueChanged.AddListener(SetSpringVisibility);
        ParticleVisibilityToggle.onValueChanged.AddListener(SetParticleVisibility);
        TextureVisibilityToggle.onValueChanged.AddListener(SetTextureVisibility);
        PlayButton.onClick.AddListener(Play);
        SetMass(MassInputField.text);
        SetMethod(MethodDropDown.value);
        SetSideLength(SideLengthInputField.text);
        SetSpacing(SpacingInputField.text);
        SetInitialX(XInputField.text);
        SetInitialY(YInputField.text);
        SetInitialZ(ZInputField.text);
    }

    public void SetMass(string s)
    {
        mass = Convert.ToSingle(s);
        if (mass == 0)
        {
            mass = 0.0001f;
            MassInputField.text = "0.0001";
        }
    }

    public void SetMethod(int i)
    {
        method = (ForceStatus)i;
    }

    public void SetInitialX(string x) => initialPosition.x = Convert.ToSingle(x);
    public void SetInitialY(string y) => initialPosition.y = Convert.ToSingle(y);
    public void SetInitialZ(string z) => initialPosition.z = Convert.ToSingle(z);

    public void SetSideLength(string l)
    {
        sideLength = Convert.ToInt32(l);
    }

    public void SetSpacing(string s)
    {
        space = Convert.ToSingle(s);
    }

    public void AddCloth()
    {
        ClothSystem cloth = Instantiate(ClothPrefab).GetComponent<ClothSystem>();
        cloth.Mass = mass;
        cloth.ForceStatus = method;
        cloth.SideCount = sideLength;
        cloth.UnitDistance = space;
        cloth.InitialPosition = initialPosition;
    }

    public void DeleteCloth()
    {
        ClothSystem cloth = ControllPntManager.Instance.NowClick.transform.parent.GetComponent<ClothSystem>();
        Destroy(cloth.gameObject);
    }

    public void EditCloth()
    {
        ClothSystem cloth = ControllPntManager.Instance.NowClick.transform.parent.GetComponent<ClothSystem>();
        cloth.Mass = mass;
        cloth.ForceStatus = method;
    }

    public void LockParticle()
    {
        ClothSystem cloth = ControllPntManager.Instance.NowClick.transform.parent.GetComponent<ClothSystem>();
        cloth.SetLockParticle(ControllPntManager.Instance.NowClick);
    }

    public void SetSpringVisibility(bool b)
    {
        ClothSystem[] clothes = FindObjectsOfType<ClothSystem>();
        foreach (ClothSystem cloth in clothes)
            cloth.SetSpringVisibility(b);
    }

    public void SetParticleVisibility(bool b)
    {
        ClothSystem[] clothes = FindObjectsOfType<ClothSystem>();
        foreach (ClothSystem cloth in clothes)
            cloth.SetParticleVisibility(b);
    }

    public void SetTextureVisibility(bool b)
    {
        ClothSystem[] clothes = FindObjectsOfType<ClothSystem>();
        foreach (ClothSystem cloth in clothes)
            cloth.SetTextureVisibility(b);
    }

    public void Play()
    {
        isPlaying = !isPlaying;
        PlayButton.GetComponentInChildren<Text>().text = isPlaying ? "Pause" : "Play";
        ClothSystem[] clothes = FindObjectsOfType<ClothSystem>();
        foreach (ClothSystem cloth in clothes)
            cloth.IsPlaying = isPlaying;
    }
}
