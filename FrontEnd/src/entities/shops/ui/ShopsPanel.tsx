import { useState } from "react";
import type { FormEvent } from "react";
import { ApiError } from "@shared/api/httpClient";
import {
  useCreateShopMutation,
  useDeleteShopMutation,
  useShopsListQuery,
} from "@entities/shops/api/queries";

export function ShopsPanel() {
  const shopsQuery = useShopsListQuery();
  const createMutation = useCreateShopMutation();
  const deleteMutation = useDeleteShopMutation();

  const [name, setName] = useState("");
  const [address, setAddress] = useState("");
  const [description, setDescription] = useState("");

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    createMutation.mutate(
      { name, address, description },
      {
        onSuccess: () => {
          setName("");
          setAddress("");
          setDescription("");
        },
      },
    );
  };

  const createError = createMutation.error as ApiError | null;

  return (
    <section>
      <h2>Shops</h2>
      <form onSubmit={onSubmit}>
        <input placeholder="Name" value={name} onChange={(event) => setName(event.target.value)} required />
        <input placeholder="Address" value={address} onChange={(event) => setAddress(event.target.value)} required />
        <input placeholder="Description" value={description} onChange={(event) => setDescription(event.target.value)} />
        <button type="submit" disabled={createMutation.isPending}>
          {createMutation.isPending ? "Saving..." : "Add shop"}
        </button>
      </form>

      {createError ? <p className="error">Create failed: {createError.message}</p> : null}

      {shopsQuery.isLoading ? <p>Loading shops...</p> : null}
      {shopsQuery.error ? <p className="error">Could not load shops.</p> : null}

      <ul>
        {shopsQuery.data?.map((shop) => (
          <li key={shop.id}>
            <strong>{shop.name}</strong> <span>{shop.address}</span>
            <button type="button" onClick={() => deleteMutation.mutate(shop.id)} disabled={deleteMutation.isPending}>
              Delete
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}
