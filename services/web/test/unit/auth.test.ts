import jwt from "jsonwebtoken";
import { extendSessionWithIdentity } from "../../src/auth";
import * as apiClient from "../../src/apiClient";

jest.mock("../../src/apiClient");

function fakeIdToken(claims: Record<string, unknown>): string {
  // afterCallback only decodes the id_token (Auth0/express-openid-connect already verified
  // its signature upstream) — an unsigned token is fine for exercising that decode step.
  return jwt.sign(claims, "unused", { algorithm: "none" });
}

describe("extendSessionWithIdentity", () => {
  const resolveIdentityMock = apiClient.resolveIdentity as jest.MockedFunction<typeof apiClient.resolveIdentity>;

  beforeEach(() => {
    resolveIdentityMock.mockReset();
  });

  it("resolves identity using the id_token's subject, email, and name claims", async () => {
    resolveIdentityMock.mockResolvedValue({ personId: "p1", roles: [], status: "Pending" });

    const session = {
      id_token: fakeIdToken({
        sub: "auth0|abc123",
        email: "ada@example.com",
        given_name: "Ada",
        family_name: "Lovelace",
      }),
    };

    await extendSessionWithIdentity(session);

    expect(resolveIdentityMock).toHaveBeenCalledWith("auth0|abc123", "ada@example.com", "Ada", "Lovelace");
  });

  it("merges personId, roles, and status into the returned session without dropping the original tokens", async () => {
    resolveIdentityMock.mockResolvedValue({ personId: "p1", roles: ["Doctor"], status: "Active" });

    const session = {
      id_token: fakeIdToken({ sub: "auth0|abc123" }),
      access_token: "some-access-token",
    };

    const result = await extendSessionWithIdentity(session);

    expect(result).toEqual({
      id_token: session.id_token,
      access_token: "some-access-token",
      personId: "p1",
      roles: ["Doctor"],
      status: "Active",
    });
  });

  it("falls back to the email prefix and 'User' when given_name/family_name are absent", async () => {
    resolveIdentityMock.mockResolvedValue({ personId: "p1", roles: [], status: "Pending" });

    const session = { id_token: fakeIdToken({ sub: "auth0|abc123", email: "grace@example.com" }) };

    await extendSessionWithIdentity(session);

    expect(resolveIdentityMock).toHaveBeenCalledWith("auth0|abc123", "grace@example.com", "grace", "User");
  });

  it("throws if the id_token has no subject claim", async () => {
    const session = { id_token: fakeIdToken({ email: "no-sub@example.com" }) };

    await expect(extendSessionWithIdentity(session)).rejects.toThrow(/subject/);
  });
});
