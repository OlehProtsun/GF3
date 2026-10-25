export function pinGeneratedColumnsFirst(order: readonly number[], generatedPseudoEmployeeIds: readonly number[]): number[] {
  const unique = [...new Set(order)];
  const generated = new Set(generatedPseudoEmployeeIds.filter(id => id < 0));
  return [...unique.filter(id => generated.has(id)), ...unique.filter(id => !generated.has(id))];
}
