using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ControllPntManager : MonoBehaviour
{
    public static ControllPntManager Instance { get; private set; }
    public GameObject NowClick { get; private set; }
    public GameObject pivot;
    public Plane clipPlane;
    private GameObject inGamePivot;
    private GameObject tmpInGamePivot;
    private GameObject selectedOb;
    public bool IsMoving { get; private set; }
    private pntController pntControllerID;

    private enum pntController
    {
        Pnt,
        yzPlane,
        xzPlane,
        xyPlane
    };

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        inGamePivot = null;
        tmpInGamePivot = null;
        selectedOb = null;
        IsMoving = false;
        pntControllerID = pntController.Pnt;
    }

    // Update is called once per frame
    void Update()
    {
        if (inGamePivot == null)
        {
            // Not moving any object.
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit))
                {
                    Transform objectHit = hit.transform;
                    if (objectHit.CompareTag("Particle"))
                    {
                        Debug.Log("1st Hit：" + objectHit.gameObject.name);
                        tmpInGamePivot = Instantiate(pivot, Vector3.zero, Quaternion.identity, objectHit);
                        tmpInGamePivot.transform.localPosition = Vector3.zero;
                        selectedOb = objectHit.gameObject;
                        NowClick = selectedOb;
                    }
                }
            } 
            else if (Input.GetMouseButtonUp(0))
            {
                inGamePivot = tmpInGamePivot;
                tmpInGamePivot = null;
            }
        }
        else
        {
            if (IsMoving)
            {
                // Object already being drag
                if (Input.GetMouseButton(0))
                {
                    // Transform coordinate
                    //Vector3 mousePos = Input.mousePosition;
                    //mousePos.z = 50;
                    //Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
                    //Debug.Log("Moving!!" + worldPosition);
                    //Debug.DrawLine(Camera.main.transform.position, worldPosition, Color.green, 5f);
                    //worldPosition.y = selectedOb.transform.position.y;
                    Debug.Log("ID：" + pntControllerID);
                    float enter = 50f;
                    if (pntControllerID == pntController.Pnt)
                        clipPlane = new Plane(Vector3.up, selectedOb.transform.position);
                    else if (pntControllerID == pntController.yzPlane)
                        clipPlane = new Plane(Vector3.forward, selectedOb.transform.position);
                    else if (pntControllerID == pntController.xzPlane)
                        clipPlane = new Plane(Vector3.left, selectedOb.transform.position);
                    else if (pntControllerID == pntController.xyPlane)
                        clipPlane = new Plane(Vector3.up, selectedOb.transform.position);

                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    clipPlane.Raycast(ray, out enter);
                    Vector3 hitPoint = ray.GetPoint(enter);

                    Vector3 oriPos = selectedOb.transform.position;
                    if (pntControllerID == pntController.yzPlane)
                    {
                        hitPoint.y = oriPos.y;
                        hitPoint.z = oriPos.z;
                    }
                    else if (pntControllerID == pntController.xzPlane)
                    {
                        hitPoint.x = oriPos.x;
                        hitPoint.z = oriPos.z;
                    }
                    else if (pntControllerID == pntController.xyPlane)
                    {
                        hitPoint.x = oriPos.x;
                        hitPoint.y = oriPos.y;
                    }
                    selectedOb.transform.position = hitPoint;
                    // Debug.DrawLine(Camera.main.transform.position, hitPoint, Color.green, 5f);
                }
                else
                {
                    IsMoving = false;
                }
            }
            else
            {
                // Object is not being drag.
                if (Input.GetMouseButton(0))
                {
                    RaycastHit hit;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    bool isHit = Physics.Raycast(ray, out hit);
                    Transform objectHit = hit.transform;
                    if (isHit)
                        Debug.Log("Hit：" + objectHit.gameObject.name);
                        //Debug.DrawLine(Camera.main.transform.position, hit.point, Color.green, 5f);
                    if (isHit && objectHit.CompareTag("ControllPnt"))
                    {
                        pntControllerID = pntController.Pnt;
                        if (objectHit.gameObject == selectedOb)
                        {
                            // Dragging object
                            IsMoving = true;
                            
                        }
                        else
                        {
                            // Select another object.
                            inGamePivot.transform.SetParent(objectHit);
                            inGamePivot.transform.localPosition = Vector3.zero;
                            selectedOb = objectHit.gameObject;
                        }
                    }
                    else if (isHit && objectHit.CompareTag("ControllAxis"))
                    {
                        if (objectHit.gameObject.name == "x_axis")
                            pntControllerID = pntController.yzPlane;
                        else if (objectHit.gameObject.name == "y_axis")
                            pntControllerID = pntController.xzPlane;
                        else if (objectHit.gameObject.name == "z_axis")
                            pntControllerID = pntController.xyPlane;
                        else
                            pntControllerID = pntController.Pnt;
                        IsMoving = true;
                    }
                    else
                    {
                        // Hit nothing
                        selectedOb = null;
                        Destroy(inGamePivot);
                        inGamePivot = null;
                    }
                }

            }
        }
    }
}
