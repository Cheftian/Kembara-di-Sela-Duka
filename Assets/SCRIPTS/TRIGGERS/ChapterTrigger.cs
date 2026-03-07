using UnityEngine;

public class ChapterTrigger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameManager.Chapter chapterToSet;
    [SerializeField] private bool triggerOnEnable = true;

    private void OnEnable()
    {
        if (triggerOnEnable)
        {
            ApplyChapterChange();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Tetap kita berikan opsi jika ingin trigger lewat sentuhan Player
        if (!triggerOnEnable && other.CompareTag("Player"))
        {
            ApplyChapterChange();
        }
    }

    private void ApplyChapterChange()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeChapter(chapterToSet);
        }
    }
}