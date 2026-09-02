using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput), typeof(Rigidbody2D), typeof(SpriteRenderer))]
[RequireComponent(typeof(SortingGroup))]
public sealed class PlayerSplitSpriteAnimator : MonoBehaviour
{
    [SerializeField] private Sprite idleSheet;
    [SerializeField] private Sprite walkingSheet;
    [SerializeField] private Sprite jumpSheet;
    [SerializeField, Min(1)] private int frameCount = 8;
    [SerializeField, Min(.1f)] private float idleFramesPerSecond = 3f;
    [SerializeField, Min(.1f)] private float walkFramesPerSecond = 10f;
    [SerializeField, Min(.1f)] private float jumpFramesPerSecond = 10f;
    [SerializeField] private float walkUpperY = 495f, walkUpperHeight = 304f, walkLowerY = 125f, walkLowerHeight = 310f;
    [SerializeField] private float idleUpperY = 617f, idleUpperHeight = 294f, idleLowerY = 307f, idleLowerHeight = 310f;
    [SerializeField] private float jumpUpperY = 513f, jumpUpperHeight = 454f, jumpLowerY = 111f, jumpLowerHeight = 358f;

    public SpriteRenderer UpperBodyRenderer => upperBody;
    public SpriteRenderer LowerBodyRenderer => lowerBody;

    private readonly Dictionary<string, Sprite> cache = new();
    private PlayerInput playerInput;
    private Rigidbody2D body;
    private SpriteRenderer sourceRenderer, upperBody, lowerBody;
    private Sprite shownSheet;
    private int shownFrame = -1, upperFrameOverride = -1;
    private bool facingLeft;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        body = GetComponent<Rigidbody2D>();
        sourceRenderer = GetComponent<SpriteRenderer>();
        idleSheet ??= sourceRenderer.sprite;
        walkingSheet ??= idleSheet;
        jumpSheet ??= walkingSheet;
        upperBody = GetOrCreatePart("UpperBody", 1);
        lowerBody = GetOrCreatePart("LowerBody", 0);
        upperBody.transform.localPosition = Vector3.zero;
        lowerBody.transform.localPosition = new Vector3(0f, -walkLowerHeight / walkingSheet.pixelsPerUnit, 0f);
        sourceRenderer.enabled = false;
    }

    private void Update()
    {
        InputAction moveAction = playerInput.actions.FindAction("Move", false);
        Vector2 move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        if (Mathf.Abs(move.x) > .01f) facingLeft = move.x < 0f;
        bool jumping = Mathf.Abs(body.linearVelocity.y) > .01f;
        bool walking = !jumping && Mathf.Abs(move.x) > .01f;
        Sprite sheet = jumping ? jumpSheet : walking ? walkingSheet : idleSheet;
        float fps = jumping ? jumpFramesPerSecond : walking ? walkFramesPerSecond : idleFramesPerSecond;
        int frame = Mathf.FloorToInt(Time.time * fps) % frameCount;
        int upperFrame = upperFrameOverride >= 0 ? upperFrameOverride : frame;
        ShowFrame(sheet, frame, upperFrame, jumping, walking);
        lowerBody.flipX = facingLeft;
        upperBody.flipX = facingLeft;
    }

    public void SetUpperBodyFrame(int frame) => upperFrameOverride = Mathf.Clamp(frame, 0, frameCount - 1);
    public void ClearUpperBodyFrameOverride() => upperFrameOverride = -1;

    private SpriteRenderer GetOrCreatePart(string name, int order)
    {
        Transform child = transform.Find(name);
        GameObject part = child != null ? child.gameObject : new GameObject(name);
        part.transform.SetParent(transform, false);
        SpriteRenderer renderer = part.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = part.AddComponent<SpriteRenderer>();
        renderer.sharedMaterial = sourceRenderer.sharedMaterial;
        renderer.sortingLayerID = sourceRenderer.sortingLayerID;
        renderer.sortingOrder = sourceRenderer.sortingOrder + order;
        return renderer;
    }

    private void ShowFrame(Sprite sheet, int frame, int upperFrame, bool jumping, bool walking)
    {
        if (sheet == shownSheet && frame == shownFrame && upperFrame == frame) return;
        float upperY = jumping ? jumpUpperY : walking ? walkUpperY : idleUpperY;
        float upperHeight = jumping ? jumpUpperHeight : walking ? walkUpperHeight : idleUpperHeight;
        float lowerY = jumping ? jumpLowerY : walking ? walkLowerY : idleLowerY;
        float lowerHeight = jumping ? jumpLowerHeight : walking ? walkLowerHeight : idleLowerHeight;
        lowerBody.sprite = GetFrameSprite(sheet, frame, false, lowerY, lowerHeight);
        upperBody.sprite = GetFrameSprite(sheet, upperFrame, true, upperY, upperHeight);
        shownSheet = sheet;
        shownFrame = frame;
    }

    private Sprite GetFrameSprite(Sprite sheet, int frame, bool upper, float y, float height)
    {
        frame = Mathf.Clamp(frame, 0, frameCount - 1);
        string key = sheet.name + ":" + (upper ? "upper:" : "lower:") + frame;
        if (cache.TryGetValue(key, out Sprite sprite)) return sprite;
        float cellWidth = sheet.texture.width / (float)frameCount;
        float left = Mathf.Round(frame * cellWidth);
        float right = Mathf.Round((frame + 1) * cellWidth);
        sprite = Sprite.Create(sheet.texture, new Rect(left, y, right - left, height), new Vector2(.5f, 0f), sheet.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        cache.Add(key, sprite);
        return sprite;
    }
}