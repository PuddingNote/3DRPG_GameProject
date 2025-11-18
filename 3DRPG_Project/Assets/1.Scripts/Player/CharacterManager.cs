using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterManager : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        CharacterMove();
    }

    private void CharacterMove()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical);
        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

        characterController.SimpleMove(inputDirection * moveSpeed);
    }

}
