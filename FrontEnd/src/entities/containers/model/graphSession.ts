import type { Graph } from "./types";

export const OPEN_GRAPH_IDS_SEARCH_PARAM = "openGraphIds";

function sanitizeGraphIds(graphIds: number[]) {
  return graphIds.reduce<number[]>((accumulator, graphId) => {
    if (!Number.isInteger(graphId) || graphId <= 0 || accumulator.includes(graphId)) {
      return accumulator;
    }

    accumulator.push(graphId);
    return accumulator;
  }, []);
}

export function parseGraphSessionIds(value: string | null) {
  if (!value) {
    return [];
  }

  return sanitizeGraphIds(
    value
      .split(",")
      .map(item => Number(item.trim())),
  );
}

export function getGraphSessionIds(search: string, currentGraphId?: number | null) {
  const params = new URLSearchParams(search);
  const graphIds = parseGraphSessionIds(params.get(OPEN_GRAPH_IDS_SEARCH_PARAM));

  if (typeof currentGraphId !== "number" || !Number.isInteger(currentGraphId) || currentGraphId <= 0) {
    return graphIds;
  }

  if (graphIds.length === 0) {
    return [currentGraphId];
  }

  if (graphIds.includes(currentGraphId)) {
    return graphIds;
  }

  return sanitizeGraphIds([...graphIds, currentGraphId]);
}

export function buildGraphSessionSearch(graphIds: number[]) {
  const sanitizedGraphIds = sanitizeGraphIds(graphIds);
  if (sanitizedGraphIds.length === 0) {
    return "";
  }

  const params = new URLSearchParams();
  params.set(OPEN_GRAPH_IDS_SEARCH_PARAM, sanitizedGraphIds.join(","));
  return `?${params.toString()}`;
}

export function resolveGraphSession(openGraphIds: number[], graphs: Graph[], currentGraph?: Graph | null) {
  const graphById = new Map(graphs.map(graph => [graph.id, graph] as const));

  if (currentGraph) {
    graphById.set(currentGraph.id, currentGraph);
  }

  const resolvedGraphs = openGraphIds
    .map(graphId => graphById.get(graphId))
    .filter((graph): graph is Graph => Boolean(graph));

  if (resolvedGraphs.length > 0) {
    return resolvedGraphs;
  }

  return currentGraph ? [currentGraph] : [];
}
