import {
  ApiError,
  approveStaff,
  getMe,
  listPendingApprovals,
  registerStaff,
  resolveIdentity,
} from "../../src/apiClient";

describe("apiClient", () => {
  const originalFetch = global.fetch;
  const originalSecret = process.env.INTERNAL_JWT_SECRET;
  const originalBaseUrl = process.env.API_BASE_URL;

  beforeEach(() => {
    process.env.INTERNAL_JWT_SECRET = "test-secret-value-at-least-32-characters-long";
    process.env.API_BASE_URL = "http://api.test";
  });

  afterEach(() => {
    global.fetch = originalFetch;
    process.env.INTERNAL_JWT_SECRET = originalSecret;
    process.env.API_BASE_URL = originalBaseUrl;
    jest.resetAllMocks();
  });

  function mockFetchOnce(response: Partial<Response> & { json?: () => Promise<unknown> }) {
    const mock = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      statusText: "OK",
      json: async () => ({}),
      text: async () => "",
      ...response,
    } as Response);
    global.fetch = mock as unknown as typeof fetch;
    return mock;
  }

  it("attaches a Bearer token and JSON content type to every request", async () => {
    const mock = mockFetchOnce({ json: async () => ({ personId: "p1", roles: [], status: "Pending" }) });

    await resolveIdentity("auth0|abc", "a@example.com", "Ada", "Lovelace");

    expect(mock).toHaveBeenCalledWith(
      "http://api.test/internal/identity/resolve",
      expect.objectContaining({
        method: "POST",
        headers: expect.objectContaining({
          "Content-Type": "application/json",
          Authorization: expect.stringMatching(/^Bearer .+/),
        }),
      }),
    );
  });

  it("sends the request body as JSON", async () => {
    const mock = mockFetchOnce({ json: async () => ({ personId: "p1", roles: [], status: "Pending" }) });

    await resolveIdentity("auth0|abc", "a@example.com", "Ada", "Lovelace");

    const [, init] = mock.mock.calls[0] as [string, RequestInit];
    expect(JSON.parse(init.body as string)).toEqual({
      auth0Subject: "auth0|abc",
      email: "a@example.com",
      firstName: "Ada",
      lastName: "Lovelace",
    });
  });

  it("throws ApiError with the response status on a non-ok response", async () => {
    mockFetchOnce({ ok: false, status: 403, text: async () => "Forbidden" });

    await expect(getMe("p1", ["Nurse"])).rejects.toMatchObject(new ApiError(403, "Forbidden"));
  });

  it("returns undefined for a 204 No Content response", async () => {
    mockFetchOnce({ ok: true, status: 204 });

    await expect(approveStaff("admin1", ["Admin"], "target1", "Doctor")).resolves.toBeUndefined();
  });

  it("registerStaff posts to /api/staff with the current person's context", async () => {
    const mock = mockFetchOnce({ ok: true, status: 201 });

    await registerStaff("p1", ["Pending"], {
      workEmail: "a@clinic.example",
      specialization: "General",
      startDate: "2026-01-01",
      educationDegree: "MD",
    });

    expect(mock).toHaveBeenCalledWith("http://api.test/api/staff", expect.objectContaining({ method: "POST" }));
  });

  it("listPendingApprovals GETs /api/staff/pending-approvals", async () => {
    const mock = mockFetchOnce({ json: async () => [] });

    await listPendingApprovals("admin1", ["Admin"]);

    expect(mock).toHaveBeenCalledWith("http://api.test/api/staff/pending-approvals", expect.objectContaining({ method: "GET" }));
  });
});
