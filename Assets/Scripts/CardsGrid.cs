using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardsGrid : MonoBehaviour
{
    public Card CardPrefab;
    public float OriginX = -1.5f;
    public float OriginY = 3.5f;
    public float StepX = 1.5f;
    public float StepY = 1.0f;

    private GameManager gameManager;  // Reference to GameManager

    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    // Instantiate all cards objects with the relevant sprites.
    // Mark them all inactive by default
    public void BuildCardsObjects(){
        for (byte id = 0; id < GameManager.NUM_CARDS; id++)
        {
            Card curCard = Instantiate(CardPrefab, new Vector3(0, 0, 0), Quaternion.identity, transform);
            curCard.id = id;
            curCard.SetCardSprite(id);
            curCard.name = string.Format("card_{0}", id);
            curCard.gameObject.SetActive(false);
        }
    }

    // public void BuildCardsGrid()
    // {
    //     for (int ind = 0; ind < 21; ind++)
    //     {
    //         Card curCard = Instantiate(CardPrefab, GetCardPosition(ind), Quaternion.identity, transform);
    //         byte id = (byte)(ind + 1);
    //         curCard.id = id;
    //         curCard.SetCardSprite(id);
    //         curCard.name = string.Format("card_{0}", id);
    //     }
    // }

    public void RemoveCard(byte id){
        GameObject go = transform.Find(string.Format("card_{0}", id)).gameObject;
        go.transform.position = new Vector3(0, 0, 0);
        go.SetActive(false);
    }

    public void DrawCards(List<byte> cardsIndices){
        if (cardsIndices.Count > gameManager.NumMaxCards){
            Debug.Log("CardsGrid::DrawCards: Cannot draw more than " + gameManager.NumMaxCards.ToString() + " cards");
            return;
        }
        for (int index = 0; index < cardsIndices.Count; index++){
            // Debug.Log("DrawCards: " + cardsIndices[index].ToString());
            GameObject go = transform.Find(string.Format("card_{0}", cardsIndices[index])).gameObject;
            go.transform.position = GetCardPosition(index);
            go.SetActive(true);
        }
    }

    // Gives the world coordinates (relative to parent) for each card index
    public Vector3 GetCardPosition(int index)
    {
        int col = index % 3;
        int row = Mathf.FloorToInt((float)index / 3.0f);
        return new Vector3(OriginX + col * StepX, OriginY - row * StepY, 0);
    }

    // Gets an indices list of the 3 selected cards
    public List<byte> GetSelectedCards()
    {
        List<byte> res = new List<byte>();
        Card[] active_cards = transform.GetComponentsInChildren<Card>(false);
        foreach (Card c in active_cards)
        {
            if (c.IsSelected())
            {
                res.Add(c.id);
            }
            if (res.Count == 3)
            {
                break;
            }
        }
        return res;
    }

    public Card GetCardById(int id){
        GameObject go = transform.Find(string.Format("card_{0}", id)).gameObject;
        return go.GetComponent<Card>();
    }

    public void UnselectAllCards(){
        Card[] active_cards = transform.GetComponentsInChildren<Card>(false);
        foreach (Card c in active_cards)
        {
            c.SetSelected(false);
        }
    }

}
