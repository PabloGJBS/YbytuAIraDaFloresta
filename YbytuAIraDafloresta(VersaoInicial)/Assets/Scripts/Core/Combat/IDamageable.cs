/// <summary>
/// Contrato para qualquer objeto que pode receber dano
/// (inimigos, objetos destrutiveis, chefes, etc).
/// Permite que o sistema de ataque do player aplique dano
/// sem conhecer o tipo concreto do alvo.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int damage);
}
