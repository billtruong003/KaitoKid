using UnityEngine;

//TEMP (Russ): Remove once testing is complete.
public class ArmMovement : MonoBehaviour
{
    [SerializeField] private Transform _leftArm;
    [SerializeField] private Transform _rightArm;

    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _amplitude = 0.1f;

    private float _time;

    private void Update()
    {
        _time += Time.deltaTime * _speed;

        float offset = Mathf.Sin(_time) * _amplitude;

        if (_leftArm != null)
        {
            Vector3 pos = _leftArm.localPosition;
            _leftArm.localPosition = new Vector3(pos.x, offset, pos.z);
        }

        if (_rightArm != null)
        {
            Vector3 pos = _rightArm.localPosition;
            _rightArm.localPosition = new Vector3(pos.x, offset, pos.z);
        }
    }


}
