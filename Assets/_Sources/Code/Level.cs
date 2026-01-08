using Sources.Code.Gameplay.Characters;
using UnityEngine;

public class Level : MonoBehaviour
{
    [SerializeField] private Transform _characterSpawnPoint;
    [SerializeField] private PlayerCharacter _playerCharacter;
    public PlayerCharacter PlayerCharacter => _playerCharacter;
    public Vector3 CharacterSpawnPosition => _characterSpawnPoint.position;
}
