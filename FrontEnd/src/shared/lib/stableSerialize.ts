function sortJsonValue(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value.map(sortJsonValue);
  }

  if (!value || typeof value !== "object") {
    return value;
  }

  return Object.keys(value as Record<string, unknown>)
    .sort((left, right) => left.localeCompare(right))
    .reduce<Record<string, unknown>>((accumulator, key) => {
      accumulator[key] = sortJsonValue((value as Record<string, unknown>)[key]);
      return accumulator;
    }, {});
}

export function stableSerialize(value: unknown) {
  return JSON.stringify(sortJsonValue(value));
}
