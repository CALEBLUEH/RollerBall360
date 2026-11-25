using UnityEngine;

public class ObstacleBehaviour : MonoBehaviour
{
    [Min(1)]
    public int damage = 1;

    [Tooltip("Particle effect spawned when this obstacle is destroyed. Uses the lecturer effect when empty.")]
    public GameObject explosionEffect;

    [Min(0.1f)]
    public float effectLifetime = 2f;

    private bool isBeingDestroyed;

    private void OnCollisionEnter(Collision collision)
    {
        PlayerController player =
            collision.transform.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        ObstacleBehaviour owner = GetDestructionOwner();
        player.TakeDamage(owner.damage);
        owner.DestroyObstacle();
    }

    public void DestroyObstacle()
    {
        ObstacleBehaviour owner = GetDestructionOwner();

        if (owner != this)
        {
            owner.DestroyObstacle();
            return;
        }

        if (isBeingDestroyed)
            return;

        isBeingDestroyed = true;
        SpawnExplosion();
        Destroy(gameObject);
    }

    private ObstacleBehaviour GetDestructionOwner()
    {
        ObstacleBehaviour owner = this;
        Transform current = transform.parent;

        while (current != null)
        {
            ObstacleBehaviour parentObstacle = current.GetComponent<ObstacleBehaviour>();
            if (parentObstacle != null)
                owner = parentObstacle;

            current = current.parent;
        }

        return owner;
    }

    private void SpawnExplosion()
    {
        GameObject effect = explosionEffect != null
            ? explosionEffect
            : Resources.Load<GameObject>("Effects/Explosion");

        if (effect == null)
            return;

        Collider obstacleCollider = GetComponentInChildren<Collider>();
        Vector3 effectPosition = obstacleCollider != null
            ? obstacleCollider.bounds.center
            : transform.position;

        GameObject particles = Instantiate(effect, effectPosition, Quaternion.identity);
        Destroy(particles, effectLifetime);
    }
}
