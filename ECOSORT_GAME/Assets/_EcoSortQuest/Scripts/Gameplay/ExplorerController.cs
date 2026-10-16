using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoSortQuest
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ExplorerController : MonoBehaviour
    {
        public Transform Visual, LeftArm, RightArm, LeftLeg, RightLeg, CarryAnchor;
        public bool CanMove;
        public Camera FollowCamera;
        CharacterController controller;
        float gait, yaw, verticalSpeed;
        void Awake()
        {
            controller=GetComponent<CharacterController>(); controller.height=1.8f;
            controller.radius=.33f; controller.center=new Vector3(0,.9f,0); controller.stepOffset=.2f;
        }
        public void Tick(float dt)
        {
            var keyboard=Keyboard.current; var mouse=Mouse.current;
            Vector2 input=Vector2.zero;
            if(CanMove && keyboard!=null)
            {
                input=new Vector2((keyboard.dKey.isPressed||keyboard.rightArrowKey.isPressed?1:0)-(keyboard.aKey.isPressed||keyboard.leftArrowKey.isPressed?1:0),
                    (keyboard.wKey.isPressed||keyboard.upArrowKey.isPressed?1:0)-(keyboard.sKey.isPressed||keyboard.downArrowKey.isPressed?1:0));
                input=Vector2.ClampMagnitude(input,1);
                if(mouse!=null && mouse.rightButton.isPressed)yaw+=mouse.delta.ReadValue().x*.15f;
            }
            var direction=Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y);
            float speed=CanMove && keyboard!=null && keyboard.leftShiftKey.isPressed?5.4f:3.6f;
            if(controller.isGrounded)verticalSpeed=-2;else verticalSpeed=Mathf.Max(-15,verticalSpeed-20*dt);
            controller.Move((direction*speed+Vector3.up*verticalSpeed)*dt);
            var p=transform.position;p.x=Mathf.Clamp(p.x,-11,11);p.z=Mathf.Clamp(p.z,-8,5);transform.position=p;
            if(direction.sqrMagnitude>.01f)Visual.rotation=Quaternion.Slerp(Visual.rotation,Quaternion.LookRotation(direction),dt*14);
            gait+=dt*speed*2.5f;float swing=input.magnitude*Mathf.Sin(gait)*25;
            LeftLeg.localRotation=Quaternion.Euler(swing,0,0);RightLeg.localRotation=Quaternion.Euler(-swing,0,0);
            LeftArm.localRotation=Quaternion.Euler(-swing*.7f,0,0);RightArm.localRotation=Quaternion.Euler(swing*.7f,0,0);
            FollowCamera.transform.position=transform.position+Quaternion.Euler(0,yaw,0)*new Vector3(0,6,-8);
            FollowCamera.transform.LookAt(transform.position+Vector3.up*.9f);
        }
    }
}
