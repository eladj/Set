using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    public byte id;

    private Set.CardProporties cardProporties;
    private bool selected = false;
    private GameManager gameManager;  // Reference to GameManager

    // private SpriteRenderer spriteRenderer;  // Sprite renderer for the card graphics

    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    public void SetCardSprite(byte card_id)
    {
        if (IsValidCardID(card_id))
        {
            Sprite sp = Resources.Load<Sprite>(string.Format("Sprites/Cards/card_{0}", card_id + 1)) as Sprite;
            GameObject CardSprite = this.gameObject.transform.GetChild(0).gameObject;
            SpriteRenderer spriteRenderer = CardSprite.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sp;
        }
        else
        {
            Debug.Log(string.Format("Invalid card id {0}", card_id));
        }
    }

    public bool IsSelected(){
        return selected;
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(new Vector3(transform.position.x, transform.position.y, 0), new Vector3(400, 200, 0));
    }

    bool IsValidCardID(byte id)
    {
        return (id >= 0 && id < GameManager.NUM_CARDS);
    }

    void OnMouseDown()
    {
        if (!selected && gameManager.numSelectedCards < 3){
            selected = true;
            gameManager.numSelectedCards++;
        }
        else if (selected) {
            selected = false;
            gameManager.numSelectedCards--;
        }
        UpdateSelectionGraphics();

        // If we selected 3 cards check if this is a set
        if (gameManager.numSelectedCards == 3){
            gameManager.CheckSelection();
        }
    }

    public void SetSelected(bool selected_){
        selected = selected_;
        UpdateSelectionGraphics();
    }

    private void UpdateSelectionGraphics(){
        this.gameObject.transform.GetChild(2).gameObject.SetActive(selected);
    }
}
