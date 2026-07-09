using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// <see cref="AttackCommand"/> only exposes <see cref="AttackCommand.Unpowered"/> for <see cref="AttackCommand.DamageProps"/>;
/// card <see cref="DamageVar.Props"/> (e.g. <see cref="ValueProp.Unblockable"/>) must be copied explicitly for combat resolution.
/// </summary>
public static class SquAttackCommandExtensions
{
	private static readonly PropertyInfo DamagePropsProperty =
		typeof(AttackCommand).GetProperty(nameof(AttackCommand.DamageProps))
		?? throw new InvalidOperationException($"Missing {nameof(AttackCommand.DamageProps)} property.");

	public static AttackCommand WithDamageProps(this AttackCommand command, ValueProp props)
	{
		DamagePropsProperty.SetValue(command, props);
		return command;
	}

	public static AttackCommand WithDamageVarProps(this AttackCommand command, DamageVar damageVar) =>
		command.WithDamageProps(damageVar.Props);
}
