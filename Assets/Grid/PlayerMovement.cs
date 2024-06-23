using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] LevelGrid levelGrid;
    [SerializeField] Vector3Int currentCoordinates;
    [SerializeField] bool canMove = false;
    [SerializeField] float cooldown = .1f;

    float nextAllowedMovement;

    Tween currentTweenMovement;

    private void Start()
    {
        InitPlayerAt0x0();
    }

    void Update()
    {
        if (Time.time < nextAllowedMovement || !canMove) return;

        if (currentTweenMovement != null)
        {
            currentTweenMovement.Kill();
            currentTweenMovement = null;
        }

        int xDelta = Mathf.RoundToInt(Mathf.Clamp(Input.GetAxis("Horizontal"),-1,1));
        int yDelta = Mathf.RoundToInt(Mathf.Clamp(Input.GetAxis("Vertical"),-1,1));

        Vector3Int inputs = new Vector3Int(xDelta, yDelta);

        if (inputs == Vector3Int.zero)
            return;

        nextAllowedMovement = Time.time + cooldown;

        var targetTileCoord = currentCoordinates + inputs;
        var targetTile = levelGrid.GetTileAtCoordinate(targetTileCoord);

        if (targetTile != null && (targetTile.Type == TileType.Room || targetTile.Type == TileType.Corridor))
        {
            currentCoordinates = targetTileCoord;
            currentTweenMovement = transform.DOMove(levelGrid.GetWorldPosFromCoord(targetTileCoord), cooldown).SetEase(Ease.Linear);
        }
    }

    [Button]
    void InitPlayerAt0x0()
    {
        currentCoordinates = Vector3Int.zero;
        canMove = true;
    }
}
