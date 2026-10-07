using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class ScaleAndToggle : MonoBehaviour
{
    [Header("Idle Pulse Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;

    [Header("Interaction Settings")]
    [SerializeField, Min(0f)] private float scaleTransitionDuration = 0.5f;
    [SerializeField, Min(0f)] private float interactionStartScaleMultiplier = 0.2f;
    [SerializeField] private float interactionScaleMultiplier = 1.5f;
    [FormerlySerializedAs("duration")]
    [SerializeField, Min(0f), Tooltip("Durasi cadangan jika referensi narasi belum dikonfigurasi.")]
    private float fallbackDuration = 2f;

    [Header("Narration Settings")]
    [SerializeField] private NarrationData narrationData;
    [SerializeField] private TMP_Text narrationText;
    [SerializeField, Min(0f)] private float narrationFadeOutDuration = 0.35f;

    [Header("Narration Text Effects")]
    [SerializeField, Min(0f)] private float narrationWaveAmplitude = 2f;
    [SerializeField, Min(0f)] private float narrationWaveFrequency = 5f;
    [SerializeField, Min(0f)] private float narrationWaveCharacterSpacing = 0.8f;
    [SerializeField, Min(0f)] private float narrationGlitchInterval = 2f;
    [SerializeField, Min(0f)] private float narrationGlitchIntervalRandomness = 1f;
    [SerializeField, Min(0f)] private float narrationGlitchDuration = 0.12f;
    [SerializeField, Min(0f)] private float narrationGlitchDurationRandomness = 0.12f;
    [SerializeField, Min(1)] private int narrationGlitchCharacterCount = 3;
    [SerializeField, Min(0f)] private float narrationGlitchShake = 2f;

    [Header("Pickup Animation")]
    [SerializeField, Min(0f)] private float pickupPopHeight = 1f;
    [SerializeField, Min(0f)] private float pickupPopDuration = 0.4f;
    [SerializeField, Min(0f)] private float pickupSecondPopHeight = 0.35f;
    [SerializeField, Min(0f)] private float pickupSecondPopDuration = 0.18f;
    [SerializeField, Min(0f)] private float pickupScaleMultiplier = 0.5f;
    [Tooltip("Kecepatan Lerp saat objek mengikuti Player.")]
    [SerializeField, Min(0f)] private float pickupMoveSpeed = 5f;
    [SerializeField, Min(0f)] private float playerContactDistance = 0.2f;

    [Header("Toggle Animation")]
    [SerializeField, Min(0.01f)] private float toggleCameraSize = 10f;
    [Tooltip("Jeda tambahan setelah kamera tiba di titik spawn sebelum objek dimunculkan.")]
    [SerializeField, Min(0f)] private float toggleCameraFocusDuration = 0.5f;
    [SerializeField, Min(0f)] private float toggleCameraArrivalTolerance = 0.15f;
    [SerializeField, Min(0.01f)] private float toggleCameraArrivalTimeout = 5f;
    [SerializeField, Min(0f)] private float toggleAnimationDuration = 0.5f;
    [SerializeField, Min(0f)] private float toggleStartBelowOffset = 0.5f;
    [SerializeField, Min(0f)] private float toggleCameraShakeMagnitude = 0.08f;

    [Header("Toggle Settings")]
    [SerializeField] private GameObject[] objectsToToggle;

    private Vector3 originalScale;
    private bool isInteracting = false;
    private bool hasInteractedBefore = false;
    private RevealerTool revealerTool;
    private NotificationTrigger notificationTrigger;
    private Transform pickupRoot;
    private Vector3 pickupRootOriginalScale;
    private PlayerController interactingPlayer;
    private bool playerInputWasBlocked;
    private CameraController interactionCameraController;
    private bool verticalCameraSizeWasEnabled;
    private bool manualMovementWasEnabled;
    private bool narrationTextWasActive;
    private bool narrationTextWasEnabled;
    private bool isTypingNarration;
    private bool isInteractionNarrationActive;
    private bool skipNarrationToNextPeriod;
    private bool narrationContinueRequested;
    private string narrationSourceText = string.Empty;
    private AudioSource typingAudioSource;
    private HashSet<int> glitchedCharacterIndices = new HashSet<int>();
    private Vector3[][] narrationOriginalVertices;

    void Awake()
    {
        originalScale = transform.localScale;
        pickupRoot = transform.parent != null ? transform.parent : transform;
        pickupRootOriginalScale = pickupRoot.localScale;
        revealerTool = GetComponent<RevealerTool>();
        notificationTrigger = GetComponent<NotificationTrigger>();

        if (narrationText != null)
        {
            narrationTextWasActive = narrationText.gameObject.activeSelf;
            narrationTextWasEnabled = narrationText.enabled;
            narrationText.enabled = false;
        }

        if (revealerTool != null)
        {
            revealerTool.enabled = false;
        }
    }

    private void OnDisable()
    {
        StopTypingSfx();
    }

    void Update()
    {
        if (isInteractionNarrationActive && Input.anyKeyDown)
        {
            if (isTypingNarration)
            {
                skipNarrationToNextPeriod = true;
            }
            else
            {
                narrationContinueRequested = true;
            }
        }

        // Cek interaksi tombol S
        if (Input.GetKeyDown(KeyCode.S) &&
            !isInteracting &&
            !hasInteractedBefore &&
            IsPlayerInsideInteractionCollider())
        {
            StartCoroutine(InteractRoutine());
        }

        // Jalankan efek membesar/mengecil otomatis jika tidak sedang interaksi
        if (!isInteracting)
        {
            HandleIdlePulse();
        }
    }

    private void LateUpdate()
    {
        if (!isInteractionNarrationActive || narrationText == null || !narrationText.enabled)
        {
            return;
        }

        ApplyNarrationTextEffects();
    }

    // Fungsi untuk membuat objek membesar/mengecil otomatis (Idle)
    private void HandleIdlePulse()
    {
        float scaleOffset = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = originalScale + new Vector3(scaleOffset, scaleOffset, 0);
    }

    // Coroutine untuk menangani efek interaksi tombol S
    private IEnumerator InteractRoutine()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        interactingPlayer = playerObject != null
            ? playerObject.GetComponent<PlayerController>()
            : null;

        if (interactingPlayer == null)
        {
            Debug.LogError("[ScaleAndToggle] PlayerController tidak ditemukan pada Player.", this);
            yield break;
        }

        playerInputWasBlocked = interactingPlayer.BlockInput;
        interactionCameraController = CameraController.Instance;
        if (interactionCameraController != null)
        {
            verticalCameraSizeWasEnabled = interactionCameraController.VerticalCameraSizeEnabled;
            manualMovementWasEnabled = interactionCameraController.ManualMovementEnabled;
            interactionCameraController.VerticalCameraSizeEnabled = false;
            interactionCameraController.ManualMovementEnabled = false;
        }

        interactingPlayer.BeginInteractionSit();
        isInteracting = true;

        if (notificationTrigger != null)
        {
            notificationTrigger.HideNotification();
            notificationTrigger.enabled = false;
        }

        hasInteractedBefore = true;

        if (revealerTool != null)
        {
            revealerTool.enabled = true;
        }

        Vector3 startScale = originalScale * interactionStartScaleMultiplier;
        Vector3 targetScale = originalScale * interactionScaleMultiplier;
        transform.localScale = startScale;

        yield return ScaleOverTime(startScale, targetScale, scaleTransitionDuration);

        if (narrationData != null && narrationText != null && NarrationManager.Instance != null)
        {
            yield return StartCoroutine(PlayInteractionNarration());
        }
        else
        {
            if (narrationData != null || narrationText != null)
            {
                Debug.LogError(
                    "[ScaleAndToggle] NarrationData, TMP_Text, dan NarrationManager harus tersedia untuk menampilkan narasi.",
                    this);
            }

            yield return new WaitForSeconds(Mathf.Max(0f, fallbackDuration));
        }

        yield return ScaleOverTime(targetScale, originalScale, scaleTransitionDuration);
        RestoreNarrationTextVisibility();

        if (revealerTool != null)
        {
            revealerTool.enabled = false;
        }

        yield return StartCoroutine(PickupSequence());
        RestoreCameraInteractionSettings();
    }

    private void RestoreCameraInteractionSettings()
    {
        if (interactionCameraController == null) return;

        interactionCameraController.VerticalCameraSizeEnabled = verticalCameraSizeWasEnabled;
        interactionCameraController.ManualMovementEnabled = manualMovementWasEnabled;
        interactionCameraController = null;
    }

    private IEnumerator PlayInteractionNarration()
    {
        NarrationManager narrationManager = NarrationManager.Instance;
        if (narrationData.dialogueSteps == null || narrationData.dialogueSteps.Length == 0)
        {
            Debug.LogError("[ScaleAndToggle] NarrationData tidak memiliki dialogueSteps.", narrationData);
            yield break;
        }

        narrationText.gameObject.SetActive(true);
        narrationText.enabled = true;
        SetNarrationText(string.Empty);
        isInteractionNarrationActive = true;
        isTypingNarration = false;
        skipNarrationToNextPeriod = false;
        narrationContinueRequested = false;
        glitchedCharacterIndices.Clear();
        StartCoroutine(GlitchNarrationText());

        for (int stepIndex = 0; stepIndex < narrationData.dialogueSteps.Length; stepIndex++)
        {
            NarrationManager.Language activeLanguage = narrationManager.CurrentLanguage;
            string text = BuildNarrationLine(
                narrationManager,
                narrationData.dialogueSteps[stepIndex],
                activeLanguage);
            int textIndex = 0;
            SetNarrationText(string.Empty);

            while (true)
            {
                if (narrationManager.CurrentLanguage != activeLanguage)
                {
                    activeLanguage = narrationManager.CurrentLanguage;
                    text = BuildNarrationLine(
                        narrationManager,
                        narrationData.dialogueSteps[stepIndex],
                        activeLanguage);
                    textIndex = 0;
                    SetNarrationText(string.Empty);
                }

                StartTypingSfx(textIndex < text.Length);
                isTypingNarration = true;
                bool languageChangedDuringTyping = false;
                while (textIndex < text.Length)
                {
                    if (narrationManager.CurrentLanguage != activeLanguage)
                    {
                        activeLanguage = narrationManager.CurrentLanguage;
                        text = BuildNarrationLine(
                            narrationManager,
                            narrationData.dialogueSteps[stepIndex],
                            activeLanguage);
                        textIndex = 0;
                        SetNarrationText(string.Empty);
                        languageChangedDuringTyping = true;
                        break;
                    }

                    if (skipNarrationToNextPeriod)
                    {
                        skipNarrationToNextPeriod = false;
                        textIndex = FindNextPeriodEnd(text, textIndex);
                        SetNarrationText(text.Substring(0, textIndex));
                        break;
                    }

                    if (text[textIndex] == '<')
                    {
                        int closeIndex = text.IndexOf('>', textIndex);
                        if (closeIndex != -1)
                        {
                            textIndex = closeIndex + 1;
                            continue;
                        }
                    }

                    SetNarrationText(text.Substring(0, textIndex + 1));
                    textIndex++;
                    yield return new WaitForSeconds(narrationManager.TypingSpeed);
                }

                isTypingNarration = false;
                StopTypingSfx();
                if (languageChangedDuringTyping)
                {
                    continue;
                }

                SetNarrationText(text.Substring(0, textIndex));
                narrationContinueRequested = false;
                while (!narrationContinueRequested)
                {
                    if (narrationManager.CurrentLanguage != activeLanguage)
                    {
                        activeLanguage = narrationManager.CurrentLanguage;
                        text = BuildNarrationLine(
                            narrationManager,
                            narrationData.dialogueSteps[stepIndex],
                            activeLanguage);
                        textIndex = 0;
                        SetNarrationText(string.Empty);
                        break;
                    }

                    yield return null;
                }

                if (narrationManager.CurrentLanguage != activeLanguage)
                {
                    continue;
                }

                if (textIndex >= text.Length)
                {
                    break;
                }
            }
        }

        isInteractionNarrationActive = false;
        yield return StartCoroutine(FadeOutInteractionNarration());
        glitchedCharacterIndices.Clear();
        SetNarrationText(string.Empty);
    }

    private IEnumerator GlitchNarrationText()
    {
        const string glitchCharacters = "%*$&#@!?/\\|";

        while (isInteractionNarrationActive)
        {
            float interval = Mathf.Max(
                0f,
                narrationGlitchInterval +
                Random.Range(-narrationGlitchIntervalRandomness, narrationGlitchIntervalRandomness));
            yield return new WaitForSeconds(interval);
            if (!isInteractionNarrationActive) yield break;

            narrationText.ForceMeshUpdate();
            TMP_TextInfo textInfo = narrationText.textInfo;
            glitchedCharacterIndices.Clear();
            List<int> eligibleCharacterIndices = new List<int>();

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
                int sourceIndex = characterInfo.index;
                if (!characterInfo.isVisible ||
                    sourceIndex < 0 ||
                    sourceIndex >= narrationSourceText.Length ||
                    char.IsWhiteSpace(narrationSourceText[sourceIndex]))
                {
                    continue;
                }

                eligibleCharacterIndices.Add(sourceIndex);
            }

            for (int i = 0; i < narrationGlitchCharacterCount && eligibleCharacterIndices.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, eligibleCharacterIndices.Count);
                glitchedCharacterIndices.Add(eligibleCharacterIndices[randomIndex]);
                eligibleCharacterIndices.RemoveAt(randomIndex);
            }

            SetNarrationText(narrationSourceText, glitchCharacters);
            float glitchDuration = Mathf.Max(
                0f,
                narrationGlitchDuration +
                Random.Range(-narrationGlitchDurationRandomness, narrationGlitchDurationRandomness));
            yield return new WaitForSeconds(glitchDuration);

            glitchedCharacterIndices.Clear();
            if (!isInteractionNarrationActive) yield break;
            SetNarrationText(narrationSourceText);
        }
    }

    private void SetNarrationText(string sourceText, string glitchCharacters = null)
    {
        narrationSourceText = sourceText;
        StringBuilder displayText = new StringBuilder(sourceText);

        if (glitchCharacters != null)
        {
            foreach (int characterIndex in glitchedCharacterIndices)
            {
                if (characterIndex >= 0 && characterIndex < displayText.Length)
                {
                    displayText[characterIndex] = glitchCharacters[
                        Random.Range(0, glitchCharacters.Length)];
                }
            }
        }

        narrationText.text = displayText.ToString();
        narrationText.ForceMeshUpdate();
        CacheNarrationOriginalVertices();
    }

    private void CacheNarrationOriginalVertices()
    {
        TMP_TextInfo textInfo = narrationText.textInfo;
        narrationOriginalVertices = new Vector3[textInfo.meshInfo.Length][];

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            Vector3[] vertices = textInfo.meshInfo[i].vertices;
            narrationOriginalVertices[i] = new Vector3[vertices.Length];
            System.Array.Copy(vertices, narrationOriginalVertices[i], vertices.Length);
        }
    }

    private void ApplyNarrationTextEffects()
    {
        TMP_TextInfo textInfo = narrationText.textInfo;
        if (narrationOriginalVertices == null ||
            narrationOriginalVertices.Length != textInfo.meshInfo.Length)
        {
            CacheNarrationOriginalVertices();
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            Vector3[] vertices = textInfo.meshInfo[i].vertices;
            Vector3[] originalVertices = narrationOriginalVertices[i];
            if (vertices.Length != originalVertices.Length)
            {
                CacheNarrationOriginalVertices();
                return;
            }

            System.Array.Copy(originalVertices, vertices, vertices.Length);
        }

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
            if (!characterInfo.isVisible) continue;

            float waveOffset = Mathf.Sin(
                Time.time * narrationWaveFrequency + i * narrationWaveCharacterSpacing)
                * narrationWaveAmplitude;
            Vector3 offset = new Vector3(0f, waveOffset, 0f);
            if (glitchedCharacterIndices.Contains(characterInfo.index))
            {
                offset += new Vector3(
                    Random.Range(-narrationGlitchShake, narrationGlitchShake),
                    Random.Range(-narrationGlitchShake, narrationGlitchShake),
                    0f);
            }

            Vector3[] vertices = textInfo.meshInfo[characterInfo.materialReferenceIndex].vertices;
            int vertexIndex = characterInfo.vertexIndex;
            vertices[vertexIndex] += offset;
            vertices[vertexIndex + 1] += offset;
            vertices[vertexIndex + 2] += offset;
            vertices[vertexIndex + 3] += offset;
        }

        narrationText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    private IEnumerator FadeOutInteractionNarration()
    {
        Color originalColor = narrationText.color;
        if (narrationFadeOutDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < narrationFadeOutDuration)
            {
                elapsed += Time.deltaTime;
                Color fadedColor = originalColor;
                fadedColor.a = originalColor.a *
                    (1f - Mathf.Clamp01(elapsed / narrationFadeOutDuration));
                narrationText.color = fadedColor;
                yield return null;
            }
        }

        Color transparentColor = originalColor;
        transparentColor.a = 0f;
        narrationText.color = transparentColor;
        narrationText.text = string.Empty;
        narrationText.color = originalColor;
    }

    private string BuildNarrationLine(
        NarrationManager narrationManager,
        NarrationData.DialogueStep step,
        NarrationManager.Language language)
    {
        string rawText = language == NarrationManager.Language.English
            ? step.dialogueEN
            : step.dialogueID;
        return narrationManager.FormatNarrationText(rawText);
    }

    private int FindNextPeriodEnd(string text, int startIndex)
    {
        int index = startIndex;
        while (index < text.Length)
        {
            if (text[index] == '<')
            {
                int closeIndex = text.IndexOf('>', index);
                if (closeIndex != -1)
                {
                    index = closeIndex + 1;
                    continue;
                }
            }
            else if (text[index] == '.')
            {
                index++;
                while (index < text.Length && text[index] == '<')
                {
                    int closeIndex = text.IndexOf('>', index);
                    if (closeIndex == -1 || text[index + 1] != '/') break;
                    index = closeIndex + 1;
                }

                return index;
            }

            index++;
        }

        return text.Length;
    }

    private void RestoreNarrationTextVisibility()
    {
        if (narrationText == null) return;

        StopTypingSfx();
        isTypingNarration = false;
        isInteractionNarrationActive = false;
        skipNarrationToNextPeriod = false;
        narrationContinueRequested = false;
        glitchedCharacterIndices.Clear();
        SetNarrationText(string.Empty);
        narrationText.enabled = narrationTextWasEnabled;
        narrationText.gameObject.SetActive(narrationTextWasActive);
    }

    private void StartTypingSfx(bool isTyping)
    {
        if (!isTyping || typingAudioSource != null || AudioManager.Instance == null) return;

        typingAudioSource = AudioManager.Instance.PlayLoopingSFX("Typing");
    }

    private void StopTypingSfx()
    {
        if (typingAudioSource == null) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopSFX(typingAudioSource);
        }
        else
        {
            typingAudioSource.Stop();
            typingAudioSource.loop = false;
        }

        typingAudioSource = null;
    }

    private IEnumerator PickupSequence()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[ScaleAndToggle] GameObject dengan tag 'Player' tidak ditemukan.", this);
            yield return StartCoroutine(FinishPlayerInteraction());
            isInteracting = false;
            yield break;
        }

        DisablePickupScriptsOnSelfAndParents();

        Vector3 startPosition = pickupRoot.position;
        Vector3 targetPosition = startPosition + Vector3.up * pickupPopHeight;
        yield return MoveRootOverTime(startPosition, targetPosition, pickupPopDuration);

        Vector3 smallScale = pickupRootOriginalScale * pickupScaleMultiplier;
        yield return SecondPopAndShrink(smallScale);

        Collider2D[] playerColliders = player.GetComponentsInChildren<Collider2D>();
        while (player != null && !IsTouchingPlayer(player.transform, playerColliders))
        {
            pickupRoot.position = Vector3.Lerp(
                pickupRoot.position,
                player.transform.position,
                Mathf.Clamp01(pickupMoveSpeed * Time.deltaTime));

            yield return null;
        }

        if (player == null)
        {
            Debug.LogError("[ScaleAndToggle] Player menghilang sebelum objek sempat menyentuhnya.", this);
            yield return StartCoroutine(FinishPlayerInteraction());
            isInteracting = false;
            yield break;
        }

        if (pickupRoot != null)
        {
            RemovePickupSpriteRenderers();
            yield return StartCoroutine(ToggleArrayObjectsWithAnimation());
            yield return StartCoroutine(FinishPlayerInteraction());
            Destroy(pickupRoot.gameObject);
        }
    }

    private IEnumerator FinishPlayerInteraction()
    {
        if (interactingPlayer == null) yield break;

        yield return StartCoroutine(interactingPlayer.EndInteractionSitAndWait());
        interactingPlayer.BlockInput = playerInputWasBlocked;
        interactingPlayer = null;
        isInteracting = false;
        RestoreCameraInteractionSettings();
    }

    private bool IsPlayerInsideInteractionCollider()
    {
        Collider2D interactionCollider = GetComponent<Collider2D>();
        if (interactionCollider == null)
        {
            interactionCollider = GetComponentInParent<Collider2D>();
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (interactionCollider == null || player == null || !interactionCollider.enabled)
        {
            return false;
        }

        Collider2D[] playerColliders = player.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider != null &&
                playerCollider.enabled &&
                interactionCollider.Distance(playerCollider).isOverlapped)
            {
                return true;
            }
        }

        return false;
    }

    private void RemovePickupSpriteRenderers()
    {
        RemoveSpriteRenderer(transform);

        if (pickupRoot != transform)
        {
            RemoveSpriteRenderer(pickupRoot);
        }
    }

    private void RemoveSpriteRenderer(Transform target)
    {
        SpriteRenderer spriteRenderer = target.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        spriteRenderer.enabled = false;
        Destroy(spriteRenderer);
    }

    private IEnumerator ToggleArrayObjectsWithAnimation()
    {
        CameraController cameraController = CameraController.Instance;
        Transform previousCameraTarget = cameraController != null
            ? cameraController.CurrentTarget
            : null;
        float previousCameraSize = cameraController != null
            ? cameraController.BaseCameraSize
            : 0f;

        if (cameraController != null)
        {
            cameraController.SetCameraSize(toggleCameraSize);
        }

        if (objectsToToggle != null)
        {
            for (int i = 0; i < objectsToToggle.Length; i++)
            {
                GameObject obj = objectsToToggle[i];
                if (obj == null) continue;

                if (obj.activeSelf)
                {
                    obj.SetActive(false);
                    continue;
                }

                if (cameraController != null)
                {
                    cameraController.SetTarget(obj.transform);
                    float elapsed = 0f;
                    while (!cameraController.HasReachedCurrentTarget(toggleCameraArrivalTolerance) &&
                           elapsed < toggleCameraArrivalTimeout)
                    {
                        elapsed += Time.deltaTime;
                        yield return null;
                    }

                    if (!cameraController.HasReachedCurrentTarget(toggleCameraArrivalTolerance))
                    {
                        Debug.LogWarning(
                            "[ScaleAndToggle] Kamera belum mencapai titik spawn sebelum batas waktu; animasi tetap dilanjutkan.",
                            this);
                    }

                    if (toggleCameraFocusDuration > 0f)
                    {
                        yield return new WaitForSeconds(toggleCameraFocusDuration);
                    }
                }

                obj.SetActive(true);

                Vector3 targetScale = obj.transform.localScale;
                Vector3 targetPosition = obj.transform.localPosition;
                obj.transform.localScale = Vector3.zero;
                obj.transform.localPosition = targetPosition + Vector3.down * toggleStartBelowOffset;

                if (cameraController != null)
                {
                    cameraController.TriggerShake(toggleAnimationDuration, toggleCameraShakeMagnitude);
                }

                yield return AnimateToggledObject(
                    obj.transform,
                    targetPosition,
                    targetScale,
                    toggleAnimationDuration);
            }
        }

        if (cameraController != null)
        {
            cameraController.SetTarget(previousCameraTarget);
            cameraController.SetCameraSize(previousCameraSize);
        }
    }

    private IEnumerator AnimateToggledObject(
        Transform toggledTransform,
        Vector3 targetPosition,
        Vector3 targetScale,
        float animationDuration)
    {
        if (animationDuration <= 0f)
        {
            toggledTransform.localPosition = targetPosition;
            toggledTransform.localScale = targetScale;
            yield break;
        }

        Vector3 startPosition = targetPosition + Vector3.down * toggleStartBelowOffset;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            if (toggledTransform == null) yield break;

            float progress = Mathf.SmoothStep(0f, 1f, elapsed / animationDuration);
            toggledTransform.localPosition = Vector3.LerpUnclamped(startPosition, targetPosition, progress);
            toggledTransform.localScale = Vector3.LerpUnclamped(Vector3.zero, targetScale, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (toggledTransform != null)
        {
            toggledTransform.localPosition = targetPosition;
            toggledTransform.localScale = targetScale;
        }
    }

    private IEnumerator SecondPopAndShrink(Vector3 targetScale)
    {
        Vector3 startPosition = pickupRoot.position;
        Vector3 targetPosition = startPosition + Vector3.up * pickupSecondPopHeight;
        Vector3 startScale = pickupRoot.localScale;
        float transitionDuration = pickupSecondPopDuration;

        if (transitionDuration <= 0f)
        {
            pickupRoot.position = targetPosition;
            pickupRoot.localScale = targetScale;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            pickupRoot.position = Vector3.LerpUnclamped(startPosition, targetPosition, progress);
            pickupRoot.localScale = Vector3.LerpUnclamped(startScale, targetScale, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }

        pickupRoot.position = targetPosition;
        pickupRoot.localScale = targetScale;
    }

    private void DisablePickupScriptsOnSelfAndParents()
    {
        Transform current = transform;
        while (current != null)
        {
            MonoBehaviour[] scripts = current.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null && script != this)
                {
                    script.enabled = false;
                }
            }

            current = current.parent;
        }
    }

    private IEnumerator MoveRootOverTime(Vector3 fromPosition, Vector3 toPosition, float transitionDuration)
    {
        if (transitionDuration <= 0f)
        {
            pickupRoot.position = toPosition;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            pickupRoot.position = Vector3.LerpUnclamped(fromPosition, toPosition, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }

        pickupRoot.position = toPosition;
    }

    private bool IsTouchingPlayer(Transform player, Collider2D[] playerColliders)
    {
        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider != null && playerCollider.enabled &&
                Vector2.Distance(transform.position, playerCollider.ClosestPoint(transform.position)) <= playerContactDistance)
            {
                return true;
            }
        }

        return Vector3.Distance(transform.position, player.position) <= playerContactDistance;
    }

    private IEnumerator ScaleOverTime(Vector3 fromScale, Vector3 toScale, float transitionDuration)
    {
        if (transitionDuration <= 0f)
        {
            transform.localScale = toScale;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            transform.localScale = Vector3.LerpUnclamped(fromScale, toScale, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = toScale;
    }

    // Fungsi untuk mengaktifkan/menonaktifkan objek di dalam array
    private void ToggleArrayObjects()
    {
        if (objectsToToggle == null || objectsToToggle.Length == 0) return;

        foreach (GameObject obj in objectsToToggle)
        {
            if (obj != null)
            {
                // Jika aktif menjadi nonaktif, jika nonaktif menjadi aktif
                obj.SetActive(!obj.activeSelf);
            }
        }
    }
}
