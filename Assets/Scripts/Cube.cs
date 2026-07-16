using UnityEngine;

public class Cube : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Скорость вращения по осям X, Y и Z (можно менять в инспекторе)
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 50f, 0);

    void Update()
    {
        var param = rotationSpeed * Time.deltaTime;

        // Вращаем объект каждый кадр с учетом прошедшего времени (Time.deltaTime)
        transform.Rotate(param);
        Debug.Log(param);
    }
}
