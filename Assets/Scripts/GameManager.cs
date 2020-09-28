// TODO:
// - Add Hint
// - Consider switching 2D collider to buttons for better performance
// - Handle wrong set
// - Add Options menu: Number of cards to draw, Show number of available sets
// - Handle end of game
// - Improve graphics: selecting cards, fonts
// - Add statstics: time to find set, found x out of y


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;  // Add the TextMesh Pro namespace to access the various functions.
using UnityEngine.SceneManagement;

namespace Set
{
    public enum Texture { Filled = 1, Stripes = 2, Hollow = 3 };
    public enum Shape { Squiggles = 1, Diamond = 2, Ovals = 3 };
    public enum Color { Red = 1, Purple = 2, Green = 3 };
    public enum Number { One = 1, Two = 2, Three = 3 };

    [System.Serializable]
    public class CardProporties
    {
        public Texture texture;
        public Shape shape;
        public Color color;
        public Number number;

        public CardProporties(Texture _texture, Shape _shape, Color _color, Number _number)
        {
            texture = _texture;
            shape = _shape;
            color = _color;
            number = _number;
        }
    }
}

public class GameManager : MonoBehaviour
{
    private int NumCardsDrawEachTurn = 3;
    private bool showAvailableSets = true;

    public int NumCardsStart = 12;
    public int NumMaxCards = 21;  // for more than 20 cards we are guarnteed to find a set
    private int NumSetsFound = 0;
    public const int NUM_CARDS = 81;

    public int numSelectedCards;  // Counts how many cards we have already selected

    public List<byte> activeCards;  // Current active cards on the table
    public List<byte> deck;  // Holds indices of all cards, shuffled
    public List<byte> garbageDeck;  // Holds cards indices which were already played

    // Cache for fast conversion from card proporties to ID
    private Dictionary<Set.CardProporties, byte> cache_proporties_to_id;
    private Dictionary<byte, Set.CardProporties> cache_id_to_proporties;
    private Dictionary<int, bool> cache_is_set;

    public CardsGrid cardsGrid;
    public TextMeshProUGUI textSetsFound;
    public TextMeshProUGUI textCardsRemaining;
    public TextMeshProUGUI textPossibleSets;

    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.HasKey("NumCardsToDraw"))
        {
            NumCardsDrawEachTurn = PlayerPrefs.GetInt("NumCardsToDraw");
        }
        if (PlayerPrefs.HasKey("ShowAvailableSets"))
        {
            showAvailableSets = PlayerPrefs.GetInt("ShowAvailableSets") != 0;
        }

        textPossibleSets.gameObject.SetActive(showAvailableSets);

        CacheIdToCardProporties();
        CacheIsSet();
        RestartGame();
    }

    void RestartGame()
    {
        // cardsGrid.BuildCardsGrid();
        numSelectedCards = 0;
        activeCards = new List<byte>();
        garbageDeck = new List<byte>();
        CreateDeck();
        cardsGrid.BuildCardsObjects();

        // Draw first 12 cards
        for (int i = 0; i < NumCardsStart; i++)
        {
            activeCards.Add(deck[i]);
            deck.RemoveAt(i);
        }
        cardsGrid.DrawCards(activeCards);
        // cardsGrid.DrawCards(new List<byte>{1, 2, 3, 4, 5});

        UpdateText();
    }

    // Builds the cache dictionaries to go from proporties to id and the other way around
    private void CacheIdToCardProporties()
    {
        Debug.Log("Cache ID to CardsProporties");
        cache_proporties_to_id = new Dictionary<Set.CardProporties, byte>();
        cache_id_to_proporties = new Dictionary<byte, Set.CardProporties>();
        byte id = 0;

        foreach (Set.Texture texture in System.Enum.GetValues(typeof(Set.Texture)))
        {
            foreach (Set.Shape shape in System.Enum.GetValues(typeof(Set.Shape)))
            {
                foreach (Set.Color color in System.Enum.GetValues(typeof(Set.Color)))
                {
                    foreach (Set.Number number in System.Enum.GetValues(typeof(Set.Number)))
                    {
                        cache_proporties_to_id.Add(new Set.CardProporties(texture, shape, color, number), id);
                        cache_id_to_proporties.Add(id, new Set.CardProporties(texture, shape, color, number));
                        id++;
                    }
                }
            }
        }
    }

    // Builds the cache dictionary to check if 3 cards are a set
    // 81^3 = 531,441 iterations
    private void CacheIsSet()
    {
        Debug.Log("Cache set checking");
        cache_is_set = new Dictionary<int, bool>();
        for (byte id1 = 0; id1 < NUM_CARDS; id1++)
        {
            for (byte id2 = 0; id2 < NUM_CARDS; id2++)
            {
                for (byte id3 = 0; id3 < NUM_CARDS; id3++)
                {
                    cache_is_set.Add(RavelMultiIndex(id1, id2, id3), IsSet(id1, id2, id3));
                }
            }
        }
        Debug.Log("Finished Caching");
    }

    // Counts the number of all available set from the current cards on the table
    // Returns a list of 3 cards indices
    private List<List<byte>> GetPossibleSets()
    {
        List<List<byte>> sets = new List<List<byte>> ();
        int numSets = 0;
        for (int i1 = 0; i1 < activeCards.Count; i1++)
        {
            for (int i2 = i1 + 1; i2 < activeCards.Count; i2++)
            {
                for (int i3 = i2 + 1; i3 < activeCards.Count; i3++)
                {
                    if (IsSet(activeCards[i1], activeCards[i2], activeCards[i3])){
                        sets.Add(new List<byte> {activeCards[i1], activeCards[i2], activeCards[i3]});
                        numSets++;
                    }
                }
            }
        }
        return sets;
    }

    // Input: 3 cards ID
    // Output: Does these cards form a set
    // Each card can be viewed as an element of \mathbb{F}_3^4
    // which basically means 4-D points of the form (a_1, a_2, a_3, a_4) where a_i=1,2,a
    // i=1,2, or 3 for each i.
    // For instance, the point 1, 2, 3, 1 could correspond to "filled diamond green three".

    // A set consists of three cards satisfying all of these conditions:
    //  They all have the same number or have three different numbers.
    //  They all have the same shape or have three different shapes.
    //  They all have the same shading or have three different shadings.
    //  They all have the same color or have three different colors.
    //  The rules of Set are summarized by: If you can sort a group of three cards into "two of ____ and one of ____", then it is not a set.
    public bool IsSet(byte id1, byte id2, byte id3)
    {
        if (id1 == id2 || id1 == id3 || id2 == id3){
            // Cards must be different
            return false;
        }
        Set.CardProporties prop1 = cache_id_to_proporties[id1];
        Set.CardProporties prop2 = cache_id_to_proporties[id2];
        Set.CardProporties prop3 = cache_id_to_proporties[id3];
        bool c1 = false, c2 = false, c3 = false, c4 = false; // The 4 conditions we need to satisfy
        if ((prop1.number == prop2.number && prop1.number == prop3.number) || (prop1.number != prop2.number && prop1.number != prop3.number && prop2.number != prop3.number))
        {
            c1 = true;
        }
        if ((prop1.shape == prop2.shape && prop1.shape == prop3.shape) || (prop1.shape != prop2.shape && prop1.shape != prop3.shape && prop2.shape != prop3.shape))
        {
            c2 = true;
        }
        if ((prop1.texture == prop2.texture && prop1.texture == prop3.texture) || (prop1.texture != prop2.texture && prop1.texture != prop3.texture && prop2.texture != prop3.texture))
        {
            c3 = true;
        }
        if ((prop1.color == prop2.color && prop1.color == prop3.color) || (prop1.color != prop2.color && prop1.color != prop3.color && prop2.color != prop3.color))
        {
            c4 = true;
        }
        if (c1 && c2 && c3 && c4)
        {
            return true;
        }
        return false;
    }

    public bool CheckSelection()
    {
        List<byte> selected_cards = cardsGrid.GetSelectedCards();
        byte[] tmp = selected_cards.ToArray();
        Debug.Log(string.Format("Selected Cards: {0}, {1}, {2}", selected_cards[0], selected_cards[1], selected_cards[2]));
        bool is_set = cache_is_set[RavelMultiIndex(selected_cards[0], selected_cards[1], selected_cards[2])];
        Debug.Log("Is set? " + is_set.ToString());
        if (is_set)
        {
            // Remove current cards
            foreach (byte n in selected_cards)
            {
                activeCards.Remove(n);
                garbageDeck.Add(n);
                cardsGrid.RemoveCard(n);
            }
            cardsGrid.DrawCards(activeCards);
            NumSetsFound++;
            numSelectedCards = 0;

            UpdateText();
        }
        return is_set;
    }

    // Converts (id1, id2, id3) into a single integer to allow hashing.
    // The assumption is that id1 is between 1-81.
    private int RavelMultiIndex(byte id1, byte id2, byte id3)
    {
        int stride = NUM_CARDS;
        int res = id1 + stride * (id2 + stride * id3);
        return res;
    }

    // private byte[] UnravelIndex(int idx){
    //     int stride = 81;
    //     int z = idx / (stride * stride);
    //     idx -= (z * stride * stride);
    //     int y = idx / stride;
    //     int x = idx % stride;
    //     return new byte[]{ (byte)(x+1), (byte)(y+1), (byte)(z+1) };
    // }

    public void CreateDeck()
    {
        deck = new List<byte>();
        for (byte i = 0; i < NUM_CARDS; i++)
        {
            deck.Add(i);
        }
        // Fisher–Yates shuffle
        for (int i = NUM_CARDS - 1; i >= 1; i--)
        {
            int j = Random.Range(0, i + 1);
            byte tmp = deck[i];
            deck[i] = deck[j];
            deck[j] = tmp;
        }
    }

    public void DrawMoreCards()
    {
        // Draw more cards
        for (int i = 0; i < NumCardsDrawEachTurn; i++)
        {
            if (deck.Count > 0 && activeCards.Count < NumMaxCards)
            {
                activeCards.Add(deck[i]);
                deck.RemoveAt(i);
            }
            else
            {
                if (deck.Count == 0)
                {
                    Debug.Log("Cannot draw more cards - Deck is empty");
                }
                if (activeCards.Count >= NumMaxCards)
                {
                    Debug.Log("Cannot draw more cards - Reached maximum number of active cards");
                }
                break;
            }
        }
        cardsGrid.DrawCards(activeCards);

        UpdateText();
    }

    public void OpenMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void UpdateText()
    {
        textSetsFound.text = "Sets Found: " + NumSetsFound.ToString();
        textCardsRemaining.text = "Cards Remaining: " + deck.Count.ToString();
        if (showAvailableSets){
            textPossibleSets.text = "Available Sets: " + GetPossibleSets().Count.ToString();
        }
    }

    public void Hint2Cards(){
        List<List<byte>> sets = GetPossibleSets();
        if (sets.Count > 0){
            // Remove all current selections
            cardsGrid.UnselectAllCards();
            numSelectedCards = 0;

            // Select first two cards
            cardsGrid.GetCardById(sets[0][0]).SetSelected(true);
            cardsGrid.GetCardById(sets[0][1]).SetSelected(true);
            numSelectedCards = 2;
        }
    }

}
