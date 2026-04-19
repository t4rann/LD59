using UnityEngine;

public class ActionButtonsController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private ActionButton3DSimple foldButton;
    [SerializeField] private ActionButton3DSimple callButton;
    [SerializeField] private ActionButton3DSimple raiseButton;
    
    [Header("Settings")]
    [SerializeField] private int defaultRaiseAmount = 10;
    
    private PlayerChips playerChips;
    private BettingController bettingController;
    private int currentBet = 0;
    private int currentRaiseAmount = 10;
    
    void Start()
    {
        playerChips = FindFirstObjectByType<PlayerChips>();
        
        if (foldButton != null)
            foldButton.OnClick += OnFoldClicked;
        
        if (callButton != null)
            callButton.OnClick += OnCallClicked;
        
        if (raiseButton != null)
            raiseButton.OnClick += OnRaiseClicked;
    }
    
    public void SetBettingController(BettingController controller)
    {
        bettingController = controller;
        
        // Получаем актуальную высоту рейза из BettingController
        if (bettingController != null)
        {
            currentRaiseAmount = bettingController.GetRaiseAmount();
            Debug.Log($"ActionButtonsController: Raise amount set to {currentRaiseAmount}");
        }
        
        Debug.Log("ActionButtonsController: BettingController set");
    }
    
    public void UpdateButtonsState(int bet)
    {
        currentBet = bet;
        
        if (playerChips == null)
        {
            playerChips = FindFirstObjectByType<PlayerChips>();
            if (playerChips == null) return;
        }
        
        // Обновляем высоту рейза из BettingController при каждом обновлении
        if (bettingController != null)
        {
            currentRaiseAmount = bettingController.GetRaiseAmount();
        }
        
        if (callButton != null)
        {
            var label = callButton.GetComponentInChildren<TMPro.TextMeshPro>();
            if (label != null)
                label.text = bet > 0 ? $"CALL {bet}" : "CHECK";
        }
        
        if (raiseButton != null)
        {
            var label = raiseButton.GetComponentInChildren<TMPro.TextMeshPro>();
            if (label != null)
                label.text = $"RAISE {bet + currentRaiseAmount}";
        }
        
        if (callButton != null)
            callButton.SetInteractable(playerChips.HasEnoughChips(bet));
        
        if (raiseButton != null)
            raiseButton.SetInteractable(playerChips.HasEnoughChips(bet + currentRaiseAmount));
    }
    
    private void OnFoldClicked(PlayerAction action)
    {
        if (bettingController != null)
        {
            bettingController.SetPlayerAction(PlayerAction.Fold);
        }
        else
        {
            Debug.LogError("BettingController is null! Cannot fold.");
        }
    }
    
    private void OnCallClicked(PlayerAction action)
    {
        if (bettingController != null && playerChips != null && playerChips.HasEnoughChips(currentBet))
        {
            bettingController.SetPlayerAction(PlayerAction.Call);
        }
        else if (bettingController == null)
        {
            Debug.LogError("BettingController is null! Cannot call.");
        }
    }
    
    private void OnRaiseClicked(PlayerAction action)
    {
        if (bettingController != null && playerChips != null && playerChips.HasEnoughChips(currentBet + currentRaiseAmount))
        {
            bettingController.SetPlayerAction(PlayerAction.Raise);
        }
        else if (bettingController == null)
        {
            Debug.LogError("BettingController is null! Cannot raise.");
        }
        else
        {
            Debug.LogWarning($"Not enough chips for raise! Need {currentBet + currentRaiseAmount}, have {playerChips.GetChips()}");
        }
    }
    
    public void ShowButtons(bool show)
    {
        if (foldButton != null) foldButton.gameObject.SetActive(show);
        if (callButton != null) callButton.gameObject.SetActive(show);
        if (raiseButton != null) raiseButton.gameObject.SetActive(show);
    }
}