using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections; // Add this line for IEnumerator
using System.Runtime.InteropServices;

public class FallingObjectsGame : MonoBehaviour
{
    public Slider scoreSlider; // Reference to the UI slider
    public int maxScore = 100; // Maximum score to win the game
    public int scoreIncrement = 10; // Score added when collecting a good object
    public int scoreDecrement = 10; // Score deducted when collecting a bad object
    public int maxMistakes = 3; // Maximum number of mistakes allowed

    public GameObject player; // Reference to the player object
    public SpriteRenderer playerSpriteRenderer; // SpriteRenderer of the player
    public Sprite[] playerSprites; // Array of sprites for the player
    private int currentSpriteIndex = 0; // Current sprite index

    public Image[] lifeImages; // Array of UI images representing player lives
    private int currentLifeIndex; // Tracks the current life image index

    public float screenBounds = 8f; // Horizontal boundaries for the player

    public GameObject goodPrefab; // Prefab for good objects
    public GameObject badPrefab; // Prefab for bad objects
    public Transform[] spawnPoints; // Array of spawn points
    public float spawnInterval = 2f; // Interval between spawns
    public float speedIncreaseRate = 0.1f; // Rate at which object speed increases

    public GameObject winScreen; // UI element for the win screen
    public GameObject loseScreen; // UI element for the lose screen
    public AudioClip winSound; // Audio clip for win
    public AudioClip loseSound; // Audio clip for lose
    public AudioClip pointSound; // Audio clip for collecting a point
    public AudioClip losspointSound; // Audio clip for collecting a point
    public AudioSource audioSource; // Audio source to play the sounds
    public Button restartButton; // Restart button
    public Button startButton; // Start button
    public GameObject startScreen; // Start screen

    private float objectSpeed = 2f; // Initial speed of falling objects
    private int currentScore = 0; // Current score
    private int mistakeCount = 0; // Count of mistakes

    private bool gameEnded = false; // Track if the game has ended
    private bool isGameStarted = false; // Track if the game has started
    public Text pointLossMessage;

    [DllImport("__Internal")]
  private static extern void SendScore(int score, int game);
    void Start()
    {
        // Initialize the slider with the starting score
        if (scoreSlider != null)
        {
            scoreSlider.maxValue = maxScore;
            scoreSlider.value = currentScore;
        }

        // Initialize the life index to point to the last life image
        currentLifeIndex = lifeImages.Length - 1;

        // Ensure win/lose screens are disabled at the start
        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);

        // Ensure restart button is disabled and add listener
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
            restartButton.onClick.AddListener(RestartGame);
        }

        // Pause the game initially and set up the start button
        Time.timeScale = 0f;
        if (startButton != null)
        {
            startButton.gameObject.SetActive(true); // Ensure button is active
            startButton.onClick.AddListener(StartGame); // Add the StartGame listener
        }
    }

    void Update()
    {
        if (!gameEnded)
        {
            HandlePlayerMovement();
        }
    }

    private void HandlePlayerMovement()
    {
        if (!isGameStarted) return; // Exit if the game hasn't started

        // Get the mouse position in world coordinates
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Set the player's x position to the mouse's x position
        Vector3 newPosition = player.transform.position;
        newPosition.x = Mathf.Clamp(mousePosition.x, -screenBounds, screenBounds);

        // Apply the new position
        player.transform.position = newPosition;
    }

    private void SpawnObject()
    {
        if (gameEnded) return;

        // Choose a random spawn point
        int spawnIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[spawnIndex];

        // Choose randomly between good and bad object
        GameObject prefabToSpawn = Random.value > 0.5f ? goodPrefab : badPrefab;

        // Instantiate the object
        GameObject fallingObject = Instantiate(prefabToSpawn, spawnPoint.position, Quaternion.identity);

        // Add a Rigidbody2D and set velocity
        Rigidbody2D rb = fallingObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.velocity = new Vector2(0, -objectSpeed);

        // Gradually increase object speed
        objectSpeed += speedIncreaseRate * Time.deltaTime;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (gameEnded) return;

        if (collision.gameObject.CompareTag("point"))
        {
            // Collect good object
            currentScore += scoreIncrement;
            if (currentScore > maxScore)
                currentScore = maxScore;

            UpdateSlider();

            // Play point collection sound
            if (audioSource != null && pointSound != null)
            {
                audioSource.PlayOneShot(pointSound);
            }

            // Update player sprite
            UpdatePlayerSprite();

            // Check for win condition
            if (currentScore >= maxScore)
            {
                WinGame();
            }

            Destroy(collision.gameObject); // Remove the collected object
        }
        else if (collision.gameObject.CompareTag("negpoint"))
        {
            // Collect bad object
            RemoveLife();

            mistakeCount++;
            currentScore -= scoreDecrement;
            if (currentScore < 0)
                currentScore = 0;

            UpdateSlider();

            // Display the remaining lives
            if (pointLossMessage != null)
            {
                int livesLeft = currentLifeIndex + 1; // Remaining lives (life index starts from 0)
                string message = livesLeft > 0
                    ? $"You have {livesLeft} lives left!"
                    : "This is your last life!";
                StartCoroutine(ShowPointLossMessage(message));
            }
            // Play point collection sound
            if (audioSource != null && pointSound != null)
            {
                audioSource.PlayOneShot(losspointSound);
            }
            // Check for game over condition
            if (mistakeCount >= maxMistakes)
            {
                GameOver();
               
            }

            Destroy(collision.gameObject); // Remove the collected object
        }
    }
    private IEnumerator ShowPointLossMessage(string message)
    {
        pointLossMessage.text = message;
        pointLossMessage.gameObject.SetActive(true);
        yield return new WaitForSeconds(1);
        pointLossMessage.gameObject.SetActive(false);
    }
    private void UpdatePlayerSprite()
    {
        if (currentSpriteIndex < playerSprites.Length - 1)
        {
            currentSpriteIndex++;
            playerSpriteRenderer.sprite = playerSprites[currentSpriteIndex];
        }
    }

    private void RemoveLife()
    {
        if (currentLifeIndex >= 0)
        {
            lifeImages[currentLifeIndex].gameObject.SetActive(false);
            currentLifeIndex--;
        }
    }

    private void UpdateSlider()
    {
        if (scoreSlider != null)
        {
            scoreSlider.value = currentScore;
        }
    }

    private void WinGame()
    {
        gameEnded = true;
        Debug.Log("You Win!");
        Debug.Log(currentScore);
        SendScore(currentScore, 8);
        if (winScreen != null) winScreen.SetActive(true);
        if (audioSource != null && winSound != null)
        {
            audioSource.clip = winSound;
            audioSource.Play();
        }

        // Enable restart button
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }

        // Stop spawning objects
        CancelInvoke("SpawnObject");
    }

    private void GameOver()
    {
        gameEnded = true;
         Debug.Log(currentScore);
          SendScore(currentScore, 8);
        Debug.Log("Game Over!");

        if (loseScreen != null) loseScreen.SetActive(true);
        if (audioSource != null && loseSound != null)
        {
            audioSource.clip = loseSound;
            audioSource.Play();
        }

        // Enable restart button
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }

        // Stop spawning objects
        CancelInvoke("SpawnObject");
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void StartGame()
    {
        startScreen.SetActive(false); // Hide the start screen
        Time.timeScale = 1f; // Resume game time
        isGameStarted = true; // Indicate that the game has started

        // Start spawning objects
        InvokeRepeating("SpawnObject", 0f, spawnInterval);
    }
}
