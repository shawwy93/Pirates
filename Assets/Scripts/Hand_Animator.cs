using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Hand_Animator : MonoBehaviour
{
    [SerializeField] private NearFarInteractor nearFarInteractor;
    [SerializeField] private SkinnedMeshRenderer handMesh;
    [SerializeField] private GameObject handArmature;
    [SerializeField] private InputActionReference selectActionRef;
    [SerializeField] private InputActionReference activateActionRef;
    [SerializeField] private Animator handAnimator;

    [SerializeField] private float actionDelay = 0.3f;

    private static readonly int activateAnim = Animator.StringToHash("activate");
    private static readonly int selectAnim = Animator.StringToHash("select");
      private static readonly int grabAnim = Animator.StringToHash("grasp");

    private bool emptyHand;

    private void Awake()
    {
        emptyHand = true;
        nearFarInteractor.selectEntered.AddListener(OnGrab);
        nearFarInteractor.selectExited.AddListener(OnRelease);
    }

    private void OnDestroy()
    {
        if (nearFarInteractor != null)
        {
            nearFarInteractor.selectEntered.RemoveListener(OnGrab);
            nearFarInteractor.selectExited.RemoveListener(OnRelease);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        Debug.Log("Selected");
        handAnimator.SetBool(grabAnim, true);
        emptyHand = false;

        handArmature.SetActive(false);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        handAnimator.SetBool(grabAnim, false);

        StartCoroutine(DelayedRelease());
    }

    private IEnumerator DelayedRelease()
    {

        yield return new WaitForSeconds(actionDelay);

        handMesh.enabled = true;
        handArmature.SetActive(true);
        emptyHand = true;
    }

    void Update()
    {
        if (emptyHand)
        {

            handAnimator.SetFloat(activateAnim, activateActionRef.action.ReadValue<float>());
            handAnimator.SetFloat(selectAnim, selectActionRef.action.ReadValue<float>());
        }
    }
}
