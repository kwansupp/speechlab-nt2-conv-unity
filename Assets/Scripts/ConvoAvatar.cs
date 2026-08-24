using UnityEngine;

public class ConvoAvatar : MonoBehaviour
{
    // variables for bobble animation
    public float bobbleRange;
    Vector3 floatY;
    float originalY;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.originalY = this.transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        floatY = transform.position;
        floatY.y = (Mathf.Sin(Time.time) * bobbleRange);
        transform.position = floatY;
    }
}
