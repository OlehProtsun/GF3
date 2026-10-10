export type SystemNewsAudience = "all" | "managers" | "employees";

export type SystemNewsMessage = {
  id: number;
  title: string;
  body: string;
  audience: SystemNewsAudience;
  imageUrl: string | null;
  videoUrl: string | null;
  isRead: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type SaveSystemNewsInput = Pick<SystemNewsMessage, "title" | "body" | "audience" | "imageUrl" | "videoUrl">;
