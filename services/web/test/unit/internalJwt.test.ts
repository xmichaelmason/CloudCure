import jwt from "jsonwebtoken";
import { mintInternalToken, INTERNAL_JWT_ISSUER, INTERNAL_JWT_AUDIENCE } from "../../src/auth/internalJwt";

const SECRET = "test-secret-value-at-least-32-characters-long";

describe("mintInternalToken", () => {
  const originalSecret = process.env.INTERNAL_JWT_SECRET;

  beforeEach(() => {
    process.env.INTERNAL_JWT_SECRET = SECRET;
  });

  afterAll(() => {
    process.env.INTERNAL_JWT_SECRET = originalSecret;
  });

  it("throws when the shared secret is not configured", () => {
    delete process.env.INTERNAL_JWT_SECRET;
    expect(() => mintInternalToken()).toThrow(/INTERNAL_JWT_SECRET/);
  });

  it("mints a token with the expected issuer and audience", () => {
    const token = mintInternalToken();
    const decoded = jwt.verify(token, SECRET) as jwt.JwtPayload;

    expect(decoded.iss).toBe(INTERNAL_JWT_ISSUER);
    expect(decoded.aud).toBe(INTERNAL_JWT_AUDIENCE);
  });

  it("omits sub and role claims when called with no arguments (the pre-login service token)", () => {
    const token = mintInternalToken();
    const decoded = jwt.verify(token, SECRET) as jwt.JwtPayload;

    expect(decoded.sub).toBeUndefined();
    expect(decoded.role).toBeUndefined();
  });

  it("includes the person id as the sub claim when provided", () => {
    const token = mintInternalToken("11111111-1111-1111-1111-111111111111");
    const decoded = jwt.verify(token, SECRET) as jwt.JwtPayload;

    expect(decoded.sub).toBe("11111111-1111-1111-1111-111111111111");
  });

  it("includes roles as an array claim, matching the .NET side's multi-value role claim expectation", () => {
    const token = mintInternalToken("11111111-1111-1111-1111-111111111111", ["Doctor"]);
    const decoded = jwt.verify(token, SECRET) as jwt.JwtPayload;

    expect(decoded.role).toEqual(["Doctor"]);
  });

  it("expires quickly (60s) so a leaked token has a small blast radius", () => {
    const token = mintInternalToken();
    const decoded = jwt.verify(token, SECRET) as jwt.JwtPayload;

    expect(decoded.exp).toBeDefined();
    expect(decoded.iat).toBeDefined();
    expect(decoded.exp! - decoded.iat!).toBe(60);
  });
});
