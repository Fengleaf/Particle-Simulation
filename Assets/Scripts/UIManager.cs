using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.IO;
using SFB;
using UnityEngine.SceneManagement;

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

    public InputField TimeStepInputField;

    public Button PlayButton;
    public Button SaveButton;
    public Button LoadButton;

    public Button ClearButton;

    private float mass;
    private ForceStatus method;
    private int sideLength;
    private float space;
    private Vector3 initialPosition;
    private float timeStep;

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
        TimeStepInputField.onEndEdit.AddListener(SetTimeStep);
        PlayButton.onClick.AddListener(Play);
        SaveButton.onClick.AddListener(Save);
        LoadButton.onClick.AddListener(Load);
        ClearButton.onClick.AddListener(ResetScene);
        SetMass(MassInputField.text);
        SetMethod(MethodDropDown.value);
        SetSideLength(SideLengthInputField.text);
        SetSpacing(SpacingInputField.text);
        SetInitialX(XInputField.text);
        SetInitialY(YInputField.text);
        SetInitialZ(ZInputField.text);
        SetTimeStep(TimeStepInputField.text);
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
        if (space == 0)
        {
            space = 0.1f;
            SpacingInputField.text = "0.1";
        }
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
        cloth.TimeStep = timeStep;
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

    public void SetTimeStep(string s)
    {
        timeStep = Convert.ToSingle(s);
    }

    public void Play()
    {
        isPlaying = !isPlaying;
        PlayButton.GetComponentInChildren<Text>().text = isPlaying ? "Pause" : "Play";
        ClothSystem[] clothes = FindObjectsOfType<ClothSystem>();
        foreach (ClothSystem cloth in clothes)
            cloth.IsPlaying = isPlaying;
    }

    public void ResetScene()
    {
        SceneManager.LoadScene(0);
    }

    public void Save()
    {
        Dictionary<string, object> configure = new Dictionary<string, object>();
        configure["Mass"] = mass;
        configure["Method"] = method;
        configure["SideLength"] = sideLength;
        configure["Space"] = space;
        configure["X"] = initialPosition.x;
        configure["Y"] = initialPosition.y;
        configure["Z"] = initialPosition.z;
        configure["TimeStep"] = timeStep;
        string json = JsonConvert.SerializeObject(configure, new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
        string s = StandaloneFileBrowser.SaveFilePanel("儲存", Application.dataPath, "setting", "json");
        if (string.IsNullOrEmpty(s))
            return;
        using (StreamWriter write = new StreamWriter(s))
        {
            write.WriteLine(json);
        }
    }

    public void Load()
    {
        Dictionary<string, object> configure = new Dictionary<string, object>();
        string[] s = StandaloneFileBrowser.OpenFilePanel("讀取", Application.dataPath, "json", false);
        if (s.Length == 0 || s[0] == "")
            return;
        using (StreamReader reader = new StreamReader(s[0]))
        {
            string file = reader.ReadToEnd();
            configure = JsonConvert.DeserializeObject<Dictionary<string, object>>(file);
            mass = Convert.ToSingle(configure["Mass"]);
            method = (ForceStatus)(Convert.ToInt32(configure["Method"]));
            sideLength = Convert.ToInt32(configure["SideLength"]);
            space = Convert.ToSingle(configure["Space"]);
            initialPosition.x = Convert.ToSingle(configure["X"]);
            initialPosition.y = Convert.ToSingle(configure["Y"]);
            initialPosition.z = Convert.ToSingle(configure["Z"]);
            timeStep = Convert.ToSingle(configure["TimeStep"]);
            MassInputField.text = mass.ToString();
            MethodDropDown.value = (int)method;
            SideLengthInputField.text = sideLength.ToString();
            SpacingInputField.text = space.ToString();
            XInputField.text = initialPosition.x.ToString();
            YInputField.text = initialPosition.y.ToString();
            ZInputField.text = initialPosition.z.ToString();
            TimeStepInputField.text = timeStep.ToString();
        }
    }
}
